namespace Proyecto_Laboratorios_Univalle.Services;

using Proyecto_Laboratorios_Univalle.Models;

/// <summary>
/// Catálogo pequeño para la demo local. Se usa porque la base normalizada actual
/// no contiene artículos de almacén; no representa todavía el contrato de stock.
/// </summary>
public static class LaboratoryPlanningDemoCatalog
{
    public static IReadOnlyList<LaboratoryPlanningDemoCatalogItem> Items { get; } =
    [
        new("PLATO-PLANO", "Plato plano redondo", "Unid.", "Utensilio", 30),
        new("COPA-MARTINI", "Copa Martini", "Unid.", "Utensilio", 40),
        new("SHOT", "Shot", "Unid.", "Utensilio", 12),
        new("MOLDE-SILICONA", "Molde de silicona", "Unid.", "Utensilio", 8),
        new("PAPEL-TOALLA", "Papel toalla Scott", "Unid.", "Misceláneos", 250),
        new("PAPEL-FILM", "Papel film 1400 metros", "Rollo", "Misceláneos", 8),
        new("GUANTES-LATEX", "Guantes de látex talla M", "Caja", "Botiquín", 9),
        new("DETERGENTE", "Detergente 5 litros", "Bidón", "Limpieza", 5)
    ];

    public static LaboratoryPlanningDemoCatalogItem? Find(string? code) =>
        Items.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
}

public sealed record LaboratoryPlanningDemoCatalogItem(
    string Code,
    string Name,
    string Unit,
    string Category,
    int AvailableStock);
