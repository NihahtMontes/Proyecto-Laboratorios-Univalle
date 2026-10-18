using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>Acciones de stock por fila (fase 2): cuándo se ofrecen, a quién y con qué URL de regreso.</summary>
public class StockActionLinksTests
{
    [Fact]
    public void NeedsAction_UnregisteredZeroOrShortage_ButNotSufficientStock()
    {
        Assert.True(StockActionLinks.NeedsAction(null, 1m));                                      // sin registro (null)
        Assert.True(StockActionLinks.NeedsAction(ArticleStockInfo.NotRegistered(1), 1m));         // sin registro
        Assert.True(StockActionLinks.NeedsAction(new ArticleStockInfo(1, StockSource.Inventory, 0), 1m));  // 0 disponible
        Assert.True(StockActionLinks.NeedsAction(new ArticleStockInfo(1, StockSource.Inventory, 3), 5m));  // 3 de 5
        Assert.False(StockActionLinks.NeedsAction(new ArticleStockInfo(1, StockSource.Inventory, 5), 5m)); // 5 de 5
    }

    [Fact]
    public void For_MissingInventoryAndAdmin_OffersRegisterStock()
    {
        var action = StockActionLinks.For(ArticleStockInfo.NotRegistered(12), 2m, articleId: 12, equipmentId: null,
            stockLaboratoryId: 7, returnUrl: "/LaboratoryOperations/Requerimientos?planId=5", isAdmin: true, canManage: false);

        Assert.NotNull(action);
        Assert.Equal("Registrar stock", action!.Label);
        Assert.StartsWith("/Catalogs/Stock/Edit/7/12?returnUrl=%2FLaboratoryOperations%2FRequerimientos%3FplanId%3D5", action.Url);
        Assert.Equal("fas fa-boxes", action.Icon);
    }

    [Fact]
    public void For_ShortInventoryAndAdmin_OffersUpdateStock()
    {
        var action = StockActionLinks.For(new ArticleStockInfo(1, StockSource.Inventory, 3), 5m, 1, null, 7,
            "/LaboratoryOperations/Disponibilidad?planId=5", isAdmin: true, canManage: true);

        Assert.NotNull(action);
        Assert.Equal("Actualizar stock", action!.Label);
    }

    [Fact]
    public void For_InventoryWithoutAdmin_ReturnsNull()
    {
        Assert.Null(StockActionLinks.For(ArticleStockInfo.NotRegistered(1), 2m, 1, null, 7, "/x", isAdmin: false, canManage: true));
        Assert.Null(StockActionLinks.For(ArticleStockInfo.NotRegistered(1), 2m, 1, null, null, "/x", isAdmin: true, canManage: false));
    }

    [Fact]
    public void For_EquipmentWithCanManage_OffersRegisterUnit_EquipmentWithoutCanManage_ReturnsNull()
    {
        var action = StockActionLinks.For(new ArticleStockInfo(1, StockSource.EquipmentUnits, 0), 1m, 1, equipmentId: 33,
            stockLaboratoryId: 7, returnUrl: "/LaboratoryOperations/Requerimientos?planId=5", isAdmin: false, canManage: true);

        Assert.NotNull(action);
        Assert.Equal("Registrar unidad", action!.Label);
        Assert.Equal("/EquipmentUnits/Create?equipmentId=33&returnUrl=%2FLaboratoryOperations%2FRequerimientos%3FplanId%3D5", action.Url);
        Assert.Equal("fas fa-plus-square", action.Icon);

        // Sin canManage (o sin equipo vinculado) no hay acción para equipo.
        Assert.Null(StockActionLinks.For(new ArticleStockInfo(1, StockSource.EquipmentUnits, 0), 1m, 1, 33, 7, "/x", false, false));
        Assert.Null(StockActionLinks.For(new ArticleStockInfo(1, StockSource.EquipmentUnits, 0), 1m, 1, null, 7, "/x", false, true));
    }

    [Fact]
    public void For_SufficientStock_ReturnsNull_EvenForAdmin()
    {
        Assert.Null(StockActionLinks.For(new ArticleStockInfo(1, StockSource.Inventory, 10), 5m, 1, null, 7, "/x", true, true));
    }

    [Fact]
    public void WithFocus_AddsReplacesAndKeepsQuery()
    {
        Assert.Equal("/LaboratoryOperations/Requerimientos?planId=5&focus=art-5",
            StockActionLinks.WithFocus("/LaboratoryOperations/Requerimientos?planId=5", 5));
        Assert.Equal("/LaboratoryOperations/Requerimientos?focus=art-5",
            StockActionLinks.WithFocus("/LaboratoryOperations/Requerimientos", 5));
        // Reemplaza un focus previo sin duplicar y conserva el resto de la query.
        Assert.Equal("/p?a=1&focus=art-5", StockActionLinks.WithFocus("/p?focus=art-3&a=1", 5));
    }
}
