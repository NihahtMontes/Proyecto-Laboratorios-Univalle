namespace Proyecto_Laboratorios_Univalle.Helpers;

/// <summary>
/// Modelo del paginador compartido (Pages/Shared/_Pager.cshtml).
/// Centraliza el marcado que antes se copiaba a mano en cada listado:
/// texto «Mostrando a–b de N», primera/última página, ventana ±2, saltos «…»
/// y flechas Anterior/Siguiente deshabilitadas en los extremos.
/// Los enlaces se generan sobre la página actual con los filtros vigentes
/// (RouteValues + PageParam) y un fragment opcional para volver a la pestaña.
/// </summary>
public class PagerModel
{
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }
    public int PageSize { get; private set; }

    /// <summary>Nombre del parámetro de querystring de página (default «pageIndex»).</summary>
    public string PageParam { get; private set; } = "pageIndex";

    /// <summary>Filtros vigentes del listado (SearchTerm, estado, pestaña…). Null = no se emite.</summary>
    public Dictionary<string, string?> RouteValues { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Fragmento opcional del enlace (pestaña destino, ej. «personal»).</summary>
    public string? Fragment { get; private set; }

    /// <summary>Etiqueta de los elementos contados («usuarios», «equipos»…).</summary>
    public string ItemLabel { get; private set; } = "registros";

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>Índice del primer elemento visible de la página (0 si no hay registros).</summary>
    public int FirstItemIndex => TotalCount == 0 ? 0 : (PageIndex - 1) * PageSize + 1;

    /// <summary>Índice del último elemento visible de la página.</summary>
    public int LastItemIndex => Math.Min(PageIndex * PageSize, TotalCount);

    public static PagerModel From<T>(
        PaginatedList<T>? list,
        IDictionary<string, string?> routeValues,
        string pageParam = "pageIndex",
        string? fragment = null,
        string itemLabel = "registros")
    {
        // Las propiedades de los PageModels llegan como nullable desde Razor: sin datos = lista vacía.
        list ??= new PaginatedList<T>(new List<T>(), 0, 1, 20);
        return new PagerModel
        {
            PageIndex = list.PageIndex,
            TotalPages = list.TotalPages,
            TotalCount = list.TotalCount,
            PageSize = list.PageSize,
            PageParam = pageParam,
            RouteValues = new Dictionary<string, string?>(routeValues, StringComparer.OrdinalIgnoreCase),
            Fragment = fragment,
            ItemLabel = itemLabel,
        };
    }
}
