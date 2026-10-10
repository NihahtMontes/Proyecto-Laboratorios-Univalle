using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>Regla "no se planifica lo que no tiene stock" (Requerimientos → Disponibilidad → aprobar).</summary>
public class PlanningStockGateTests
{
    private static ArticleAvailabilityService Availability(TestDb db) =>
        new(db.Context, new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance));

    [Fact]
    public void IsMissing_UnregisteredOrZeroInventory_ButNotEquipmentOrPositiveStock()
    {
        Assert.True(PlanningStockGate.IsMissing(null));
        Assert.True(PlanningStockGate.IsMissing(ArticleStockInfo.NotRegistered(1)));
        Assert.True(PlanningStockGate.IsMissing(new ArticleStockInfo(1, StockSource.Inventory, 0)));
        Assert.False(PlanningStockGate.IsMissing(new ArticleStockInfo(1, StockSource.Inventory, 0.5m)));
        // Equipos: su disponibilidad depende de préstamos y mantenimientos; no bloquean por esta regla.
        Assert.False(PlanningStockGate.IsMissing(new ArticleStockInfo(1, StockSource.EquipmentUnits, 0)));
    }

    [Fact]
    public async Task FindAsync_ListsOnlyArticlesWithoutStock_OncePerArticle()
    {
        using var db = new TestDb();
        db.AddPlanning(1, TestDb.Week1Monday, 1, 2);
        db.AddArticleWithStock(10, "HAR", "Harina", 25);       // con stock
        db.AddArticleWithStock(11, "AZU", "Azúcar", 0);        // registrado en 0
        db.AddArticleWithStock(12, "VAI", "Vainilla", null);   // sin registro
        db.AddRequirement(1, 10, 2);
        db.AddRequirement(1, 11, 1);
        db.AddRequirement(1, 12, 1);
        db.AddRequirement(1, 12, 3); // segunda línea del mismo artículo

        var missing = await PlanningStockGate.FindAsync(db.Context, Availability(db), 1);

        Assert.Equal(["Azúcar", "Vainilla"], missing.Select(m => m.ArticleName));
    }

    [Fact]
    public async Task FindAsync_PlanningWithStock_ReturnsEmpty()
    {
        using var db = new TestDb();
        db.AddPlanning(1, TestDb.Week1Monday, 1, 2);
        db.AddArticleWithStock(10, "HAR", "Harina", 25);
        db.AddRequirement(1, 10, 2);

        Assert.Empty(await PlanningStockGate.FindAsync(db.Context, Availability(db), 1));
    }

    [Fact]
    public void StockEditUrl_EncodesReturnUrl()
    {
        var url = PlanningStockGate.StockEditUrl(7, 12, "/LaboratoryOperations/Requerimientos?planId=5");

        Assert.Equal("/Catalogs/Stock/Edit/7/12?returnUrl=%2FLaboratoryOperations%2FRequerimientos%3FplanId%3D5", url);
    }
}
