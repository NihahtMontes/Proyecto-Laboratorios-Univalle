using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Shared;

/// <summary>
/// Configuración del buscador de requerimientos (partial _RequirementPicker): modal amplio con pestañas Artículos y Equipos.
/// Las búsquedas y fichas se piden a /Api/RequirementPicker.
/// </summary>
/// <param name="Mode">
/// "units": equipos por unidad física (práctica; requiere <paramref name="PlanId"/>; envía Batch.UnitIds[i]).
/// "catalog": equipos del catálogo con cantidad (kit de la materia; envía Batch.Equipments[i].Id/Quantity).
/// Los artículos siempre se envían como Batch.Articles[i].Id/Quantity.
/// </param>
public sealed record RequirementPickerOptions(
    string Mode,
    string Title,
    int? PlanId,
    string SubmitUrl,
    IReadOnlyList<RequirementPickerService.CategoryOption> Categories,
    IReadOnlyList<RequirementPickerOptions.Option> Laboratories,
    int? DefaultLaboratoryId)
{
    public const string UnitsMode = "units";
    public const string CatalogMode = "catalog";
    public const string ApiUrl = "/Api/RequirementPicker";

    public sealed record Option(int Id, string Name);
}
