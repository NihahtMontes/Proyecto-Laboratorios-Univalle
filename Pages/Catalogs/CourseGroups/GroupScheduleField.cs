using System.Text.Json;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Pages.Shared;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.CourseGroups;

/// <summary>
/// Campo "Horario de clases" de Crear/Editar grupo: grilla lunes–sábado × periodos donde se marcan
/// las celdas de clase. Viaja como JSON en un input oculto: [{"weekday":1,"order":3}, ...].
/// </summary>
public sealed class GroupScheduleField
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GroupScheduleField(IReadOnlyList<ClassPeriod> periods, string? slotsJson, string inputName)
    {
        Periods = periods;
        SlotsJson = string.IsNullOrWhiteSpace(slotsJson) ? "[]" : slotsJson;
        InputName = inputName;
    }

    public IReadOnlyList<ClassPeriod> Periods { get; }
    public string SlotsJson { get; }
    public string InputName { get; }

    public PeriodWeekGridModel GridModel => new(
        Periods.Select(p => new PeriodWeekGridPeriod(p.Order, p.Label)).ToList(),
        PeriodWeekGridModel.DefaultDayNames);

    private sealed record SlotDto(int Weekday, int Order);

    /// <summary>Celdas válidas del JSON (día 1..6, sin duplicados). JSON inválido = null.</summary>
    public static List<WeekSlot>? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var dtos = JsonSerializer.Deserialize<List<SlotDto>>(json, JsonOptions) ?? [];
            return dtos
                .Where(d => d.Weekday is >= 1 and <= 6 && d.Order > 0)
                .Select(d => new WeekSlot(d.Weekday, d.Order))
                .Distinct()
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string Serialize(IEnumerable<WeekSlot> slots) =>
        JsonSerializer.Serialize(slots.Select(s => new SlotDto(s.Weekday, s.PeriodOrder)), JsonOptions);
}
