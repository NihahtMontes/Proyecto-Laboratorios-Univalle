using System.Globalization;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>Acción de stock ofrecida en una fila: a dónde ir y cómo se ve el botón.</summary>
public sealed record StockAction(string Url, string Label, string Icon, string Style);

/// <summary>
/// Botones de "actualizar stock" de las filas de Requerimientos y Disponibilidad. Estático y puro:
/// decide la acción según el stock, lo pedido y los roles; no toca EF ni HttpContext.
/// </summary>
public static class StockActionLinks
{
    /// <summary>True si la fila necesita acción: sin registro, disponible &lt;= 0, o disponible &lt; pedido.</summary>
    public static bool NeedsAction(ArticleStockInfo? stock, decimal requested) =>
        stock == null
        || stock.Source == StockSource.None
        || stock.Available is not > 0
        || stock.Available.Value < requested;

    /// <summary>
    /// Acción para la fila o null si no corresponde o el usuario no tiene permiso.
    /// Equipo (por fuente o vínculo): registrar otra unidad física, requiere canManage y equipmentId.
    /// Inventario o sin registro: registrar/actualizar existencias en el Economato, requiere isAdmin y laboratorio.
    /// </summary>
    public static StockAction? For(ArticleStockInfo? stock, decimal requested, int articleId, int? equipmentId,
        int? stockLaboratoryId, string returnUrl, bool isAdmin, bool canManage)
    {
        if (!NeedsAction(stock, requested)) return null;

        if (stock?.Source == StockSource.EquipmentUnits || equipmentId.HasValue)
        {
            if (!canManage || !equipmentId.HasValue) return null;
            return new StockAction(
                $"/EquipmentUnits/Create?equipmentId={equipmentId.Value}&returnUrl={Uri.EscapeDataString(returnUrl)}",
                "Registrar unidad", "fas fa-plus-square", "btn-outline-danger");
        }

        if (!isAdmin || !stockLaboratoryId.HasValue) return null;
        return new StockAction(
            PlanningStockGate.StockEditUrl(stockLaboratoryId.Value, articleId, returnUrl),
            PlanningStockGate.IsMissing(stock) ? "Registrar stock" : "Actualizar stock",
            "fas fa-boxes", "btn-outline-danger");
    }

    /// <summary>Devuelve la URL local con el parámetro focus=art-{articleId}, reemplazando un focus previo.</summary>
    public static string WithFocus(string localUrl, int articleId)
    {
        var queryStart = localUrl.IndexOf('?');
        var path = queryStart < 0 ? localUrl : localUrl[..queryStart];
        var kept = new List<string>();
        if (queryStart >= 0)
            kept.AddRange(localUrl[(queryStart + 1)..]
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(pair => !pair.StartsWith("focus=", StringComparison.Ordinal)));
        kept.Add("focus=art-" + articleId.ToString(CultureInfo.InvariantCulture));
        return path + "?" + string.Join("&", kept);
    }
}
