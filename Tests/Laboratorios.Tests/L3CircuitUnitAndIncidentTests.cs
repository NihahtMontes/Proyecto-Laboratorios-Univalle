using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>
/// Salidas L-3 con unidades físicas (O7: el ítem lleva EquipmentUnitId y la unidad figura como prestada)
/// y tope de incidentes L-5 por artículo (O8: lo registrado no puede superar lo entregado).
/// </summary>
public class L3CircuitUnitAndIncidentTests
{
    private const int EquipmentId = 20;
    private const int UnitId = 21;
    private const int ArticleId = 30;

    private static L3CircuitService Circuit(TestDb db) =>
        new(db.Context, NullLogger<L3CircuitService>.Instance);

    private static ArticleAvailabilityService Availability(TestDb db) =>
        new(db.Context, new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance));

    /// <summary>Planificación aprobada con una línea de unidad física y Economato como origen.</summary>
    private static PlanningRequirement SeedUnitPlan(TestDb db, int planningId, int startOrder = 1)
    {
        db.AddEconomato();
        db.AddPlanning(planningId, TestDb.Week1Monday, startOrder, 2, status: PlanningStatus.Approved);
        db.AddEquipmentWithUnit(EquipmentId, UnitId, "46019", ArticleId, "EQ-MIC", "Microscopio");
        return db.AddUnitRequirement(planningId, ArticleId, UnitId);
    }

    [Fact]
    public async Task CreateDeparture_LineaDeUnidad_RegistraLaUnidadSinAvisosNiIncidentes()
    {
        using var db = new TestDb();
        var detail = SeedUnitPlan(db, 1);

        var result = await Circuit(db).CreateDepartureAsync(1, TestDb.EconomatoId, [new DepartureLine(detail.Id, 1m)], null);

        Assert.True(result.Ok, result.Message);
        Assert.Empty(result.Warnings ?? []);

        var departure = await db.Context.Departures.AsNoTracking()
            .Include(d => d.Items)
            .SingleAsync(d => d.PlanningId == 1);
        var item = Assert.Single(departure.Items);
        Assert.Equal(UnitId, item.EquipmentUnitId);
        // El ítem referencia la unidad O el artículo (CK_DepartureItems_ExactlyOneReference), nunca ambos.
        Assert.Null(item.ArticleId);
        Assert.NotNull(item.Observations);
        Assert.Contains("Unidad 46019", item.Observations);

        Assert.Empty(await db.Context.DataQualityIssues.AsNoTracking()
            .Where(q => q.IssueCode == "STOCK_NOT_REGISTERED").ToListAsync());

        // La disponibilidad se deduce de la salida abierta: el estado de la unidad no cambia.
        var unit = await db.Context.EquipmentUnits.AsNoTracking().SingleAsync(u => u.Id == UnitId);
        Assert.Equal(EquipmentStatus.Operational, unit.CurrentStatus);
    }

    [Fact]
    public async Task GetStock_DespuésDeLaSalidaDeUnidad_LaMuestraComoPrestada()
    {
        using var db = new TestDb();
        var detail = SeedUnitPlan(db, 1);

        var before = await Availability(db).GetStockAsync([ArticleId], null);
        Assert.Equal(1m, before[ArticleId].Available);
        Assert.Equal(0, before[ArticleId].OnLoan);

        var result = await Circuit(db).CreateDepartureAsync(1, TestDb.EconomatoId, [new DepartureLine(detail.Id, 1m)], null);
        Assert.True(result.Ok, result.Message);

        var after = await Availability(db).GetStockAsync([ArticleId], null);
        Assert.Equal(StockSource.EquipmentUnits, after[ArticleId].Source);
        Assert.Equal(0m, after[ArticleId].Available);
        Assert.Equal(1, after[ArticleId].OnLoan);
    }

    [Fact]
    public async Task CreateDeparture_UnidadEnOtraSalidaAbierta_RechazaLaSegundaSalida()
    {
        using var db = new TestDb();
        var detail1 = SeedUnitPlan(db, 1);
        db.AddPlanning(2, TestDb.Week1Monday, 3, 2, status: PlanningStatus.Approved);
        var detail2 = db.AddUnitRequirement(2, ArticleId, UnitId);

        var first = await Circuit(db).CreateDepartureAsync(1, TestDb.EconomatoId, [new DepartureLine(detail1.Id, 1m)], null);
        Assert.True(first.Ok, first.Message);

        var second = await Circuit(db).CreateDepartureAsync(2, TestDb.EconomatoId, [new DepartureLine(detail2.Id, 1m)], null);

        Assert.False(second.Ok);
        Assert.Contains("no está disponible", second.Message);
        Assert.False(await db.Context.Departures.AsNoTracking().AnyAsync(d => d.PlanningId == 2));
    }

    [Fact]
    public async Task CreateDeparture_UnidadConCantidadDos_RechazaLaSalida()
    {
        using var db = new TestDb();
        var detail = SeedUnitPlan(db, 1);

        var result = await Circuit(db).CreateDepartureAsync(1, TestDb.EconomatoId, [new DepartureLine(detail.Id, 2m)], null);

        Assert.False(result.Ok);
        Assert.Contains("se entrega de a una", result.Message);
        Assert.False(await db.Context.Departures.AsNoTracking().AnyAsync(d => d.PlanningId == 1));
    }

    [Fact]
    public async Task CreateDeparture_ArticuloSinStockSinSerEquipo_SigueConAvisoEIncidencia()
    {
        using var db = new TestDb();
        db.AddEconomato();
        db.AddPlanning(1, TestDb.Week1Monday, 1, 2, status: PlanningStatus.Approved);
        db.AddArticleWithStock(50, "REB", "Reactivo X", stock: null);   // artículo de inventario sin fila de stock
        db.AddRequirement(1, 50, 3);
        var detail = await db.Context.PlanningRequirements.AsNoTracking()
            .SingleAsync(d => d.PlanningId == 1 && d.ArticleId == 50);

        var result = await Circuit(db).CreateDepartureAsync(1, TestDb.EconomatoId, [new DepartureLine(detail.Id, 3m)], null);

        Assert.True(result.Ok, result.Message);
        Assert.NotNull(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("no tiene stock registrado"));
        var issue = await db.Context.DataQualityIssues.AsNoTracking()
            .SingleAsync(q => q.IssueCode == "STOCK_NOT_REGISTERED" && q.EntityKey!.EndsWith("|REB"));
        Assert.Equal(DataQualityIssueSeverity.Warning, issue.Severity);
    }

    [Theory]
    [InlineData(2d, 0, 2)]
    [InlineData(2d, 2, 0)]
    [InlineData(2d, 3, 0)]
    [InlineData(0.5d, 0, 1)]
    [InlineData(2.5d, 1, 2)]
    public void MaxAdditional_TopePendienteDeIncidentes(double delivered, int alreadyRegistered, int expected)
    {
        Assert.Equal(expected, L5Rules.MaxAdditional((decimal)delivered, 0m, alreadyRegistered));
    }
}
