using Proyecto_Laboratorios_Univalle.Pages.Catalogs.CourseGroups;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>Lectura del horario por periodos que envía la grilla de Crear/Editar grupo.</summary>
public class GroupScheduleFieldTests
{
    [Fact]
    public void Parse_ReadsCells_IgnoresInvalidAndDuplicates()
    {
        var slots = GroupScheduleField.Parse(
            """[{"weekday":1,"order":2},{"weekday":1,"order":2},{"weekday":7,"order":1},{"weekday":3,"order":0},{"weekday":6,"order":12}]""");

        Assert.NotNull(slots);
        Assert.Equal([new WeekSlot(1, 2), new WeekSlot(6, 12)], slots);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[]")]
    public void Parse_EmptyMeansNoSchedule(string? json)
    {
        Assert.Empty(GroupScheduleField.Parse(json)!);
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsNull()
    {
        Assert.Null(GroupScheduleField.Parse("{no es json"));
    }

    [Fact]
    public void Serialize_RoundTrips()
    {
        var json = GroupScheduleField.Serialize([new WeekSlot(2, 4), new WeekSlot(5, 1)]);

        Assert.Equal("""[{"weekday":2,"order":4},{"weekday":5,"order":1}]""", json);
        Assert.Equal([new WeekSlot(2, 4), new WeekSlot(5, 1)], GroupScheduleField.Parse(json));
    }
}
