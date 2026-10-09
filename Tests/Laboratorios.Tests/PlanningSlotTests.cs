using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

public class PlanningSlotTests
{
    private static readonly ClassPeriod[] Catalog =
    [
        new() { Id = 1, Order = 1, StartTime = new TimeSpan(7, 35, 0), EndTime = new TimeSpan(8, 25, 0) },
        new() { Id = 2, Order = 2, StartTime = new TimeSpan(8, 35, 0), EndTime = new TimeSpan(9, 25, 0) },
        new() { Id = 3, Order = 3, StartTime = new TimeSpan(9, 25, 0), EndTime = new TimeSpan(10, 15, 0) },
        new() { Id = 4, Order = 4, StartTime = new TimeSpan(10, 25, 0), EndTime = new TimeSpan(11, 15, 0) },
    ];

    [Theory]
    [InlineData(11, 3, false)]
    [InlineData(10, 3, true)]
    [InlineData(1, 0, false)]
    [InlineData(12, 1, true)]
    [InlineData(0, 1, false)]
    public void Fits(int start, int count, bool expected) => Assert.Equal(expected, PlanningSlot.Fits(start, count));

    [Fact]
    public void Overlaps_AdjacentBlocksDoNotOverlap() => Assert.False(PlanningSlot.Overlaps(1, 4, 5, 1));

    [Fact]
    public void Overlaps_SharedLastPeriodOverlaps() => Assert.True(PlanningSlot.Overlaps(4, 1, 1, 4));

    [Fact]
    public void Apply_WritesDerivedHours()
    {
        var planning = new Planning();
        var tuesday = new DateTime(2026, 10, 6);

        PlanningSlot.Apply(planning, tuesday, startPeriodId: 2, count: 3, Catalog);

        Assert.Equal(2, planning.StartPeriodId);
        Assert.Equal(3, planning.PeriodCount);
        Assert.Equal(new TimeSpan(8, 35, 0), planning.StartTime);
        Assert.Equal(new TimeSpan(11, 15, 0), planning.EndTime);
        Assert.Equal(tuesday.Add(new TimeSpan(8, 35, 0)), planning.ScheduledDate);
        Assert.Equal(tuesday.Add(new TimeSpan(11, 15, 0)), planning.ScheduledEndDate);
    }

    [Fact]
    public void Apply_SundayThrows()
    {
        var sunday = new DateTime(2026, 10, 11);
        Assert.Throws<ArgumentException>(() => PlanningSlot.Apply(new Planning(), sunday, 1, 1, Catalog));
    }

    [Fact]
    public void DemandPeak_AdjacentPracticesDoNotAdd()
    {
        var (peak, _, _) = DemandPeak.Compute(
        [
            new TimedDemand(5, new TimeSpan(8, 0, 0), new TimeSpan(9, 0, 0)),
            new TimedDemand(4, new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0)),
            new TimedDemand(3, new TimeSpan(8, 30, 0), new TimeSpan(9, 30, 0)),
        ]);
        Assert.Equal(8, peak);
    }
}
