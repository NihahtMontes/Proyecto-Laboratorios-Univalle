using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Regla de planificación: un artículo de inventario sin stock registrado (o con 0 en todo el sistema) no se
/// puede planificar. El usuario debe registrar su existencia antes de continuar o aprobar; la pantalla de
/// existencias lo devuelve a la planificación al guardar o cancelar.
/// Los equipos (unidades físicas) no entran en esta regla: su disponibilidad depende de préstamos y mantenimientos.
/// </summary>
public static class PlanningStockGate
{
    public sealed record MissingStock(int ArticleId, string ArticleCode, string ArticleName);

    /// <summary>True si el artículo no tiene stock con el que planificar (sin registro o 0 en inventario).</summary>
    public static bool IsMissing(ArticleStockInfo? stock) =>
        stock == null
        || stock.Source == StockSource.None
        || (stock.Source == StockSource.Inventory && stock.Available is not > 0);

    /// <summary>Artículos de la planificación sin stock (total institucional), ordenados por nombre.</summary>
    public static async Task<List<MissingStock>> FindAsync(ApplicationDbContext context, IArticleAvailabilityService availability,
        int planningId)
    {
        var articles = await context.PlanningRequirements.AsNoTracking()
            .Where(d => d.PlanningId == planningId)
            .Select(d => new { d.ArticleId, d.Article!.Code, d.Article.Name })
            .Distinct()
            .ToListAsync();
        if (articles.Count == 0) return [];

        var stock = await availability.GetStockAsync(articles.Select(a => a.ArticleId), null);
        return articles
            .Where(a => IsMissing(stock.GetValueOrDefault(a.ArticleId)))
            .OrderBy(a => a.Name)
            .Select(a => new MissingStock(a.ArticleId, a.Code, a.Name))
            .ToList();
    }

    /// <summary>Laboratorio donde se registra el stock faltante: el Economato; si no existe, el de la planificación.</summary>
    public static async Task<int?> StockLaboratoryIdAsync(ApplicationDbContext context, int planningId)
    {
        var economato = await context.Laboratories.AsNoTracking()
            .Where(l => l.Code == L3CircuitService.DefaultOriginLaboratoryCode)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();
        return economato ?? await context.Plannings.AsNoTracking()
            .Where(p => p.Id == planningId)
            .Select(p => p.LaboratoryId)
            .FirstOrDefaultAsync();
    }

    /// <summary>Ruta de la pantalla de existencias que vuelve a <paramref name="returnUrl"/> al guardar o cancelar.</summary>
    public static string StockEditUrl(int laboratoryId, int articleId, string returnUrl) =>
        $"/Catalogs/Stock/Edit/{laboratoryId}/{articleId}?returnUrl={Uri.EscapeDataString(returnUrl)}";
}
