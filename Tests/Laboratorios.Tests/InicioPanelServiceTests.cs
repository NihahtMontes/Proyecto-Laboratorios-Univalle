using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.Inicio;

namespace Laboratorios.Tests;

/// <summary>
/// InicioPanelService: la semana del panel va de lunes a sábado (con hoy marcado), excluye
/// planificaciones borradas o en estados que no ocupan ambiente, y la serie semanal trae
/// 8 semanas (5 anteriores + actual + 2 siguientes) marcando la actual.
/// </summary>
public class InicioPanelServiceTests
{
    // Semana de referencia: lunes 09/11/2026 a sábado 14/11/2026; hoy es miércoles 11/11/2026.
    private static readonly DateTime Monday = new(2026, 11, 9);
    private static readonly DateTime Today = new(2026, 11, 11);

    private static InicioPanelService Service(TestDb db) => new(db.Context);

    [Fact]
    public async Task Semana_LunesASabado_AgrupaPorDiaYMarcaHoy()
    {
        using var db = new TestDb();
        db.AddPlanning(1, Monday, 1, 2);               // lunes
        db.AddPlanning(2, Today, 3, 1);                // miércoles (hoy)
        db.AddPlanning(3, Monday.AddDays(5), 1, 1);    // sábado
        // Domingo (legado, sin periodos): pertenece a la semana siguiente, no a la laboral.
        db.AddLegacyPlanning(4, Monday.AddDays(6), new TimeSpan(8, 0, 0), new TimeSpan(9, 0, 0));

        var panel = await Service(db).GetWeekAsync(Today);

        Assert.Equal(Monday, panel.Monday);
        Assert.Equal(Monday.AddDays(5), panel.Saturday);
        Assert.Equal(Today, panel.Today);
        Assert.Equal(6, panel.Days.Count);

        // Seis días seguidos lunes a sábado, con hoy marcado una sola vez.
        for (var i = 0; i < 6; i++)
        {
            Assert.Equal(Monday.AddDays(i), panel.Days[i].Date);
            Assert.Equal(i + 1, panel.Days[i].Weekday);
        }
        Assert.Equal("Lunes", panel.Days[0].DayName);
        Assert.Equal("Sábado", panel.Days[5].DayName);
        Assert.Equal("Hoy · Miércoles 11", panel.Days[2].HeaderText);
        Assert.Equal("Lunes 9", panel.Days[0].HeaderText);
        Assert.Equal(Today, panel.Days.Single(d => d.IsToday).Date);
        Assert.True(panel.Days[0].IsPast);
        Assert.False(panel.Days[2].IsPast);
        Assert.False(panel.Days[3].IsPast);

        // El domingo quedó fuera: 3 prácticas en la semana y 1 hoy.
        Assert.Equal(3, panel.Days.Sum(d => d.Practices.Count));
        Assert.Equal(1, panel.TodayCount);
    }

    [Fact]
    public async Task Semana_ExcluyeBorradasYEstadosQueNoOcupan()
    {
        using var db = new TestDb();
        db.AddPlanning(1, Today, 1, 2);                                       // Planificada: ocupa
        db.AddPlanning(2, Today, 4, 1, status: PlanningStatus.Cancelled);     // no ocupa
        db.AddPlanning(3, Today, 6, 1, status: PlanningStatus.Requested);     // no ocupa
        db.AddPlanning(4, Today, 8, 1, status: PlanningStatus.Rescheduled);   // no ocupa
        db.Context.Plannings.Add(new Planning
        {
            Id = 5,
            ManagementId = TestDb.ManagementId,
            LaboratoryId = TestDb.LabA,
            TeacherPersonId = TestDb.Teacher1,
            PracticeName = "Borrada",
            GroupName = "A",
            Status = PlanningStatus.Approved,
            RequestDate = Today,
            ScheduledDate = Today.AddHours(8),
            IsDeleted = true
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var panel = await Service(db).GetWeekAsync(Today);

        var row = Assert.Single(panel.Days[2].Practices);
        Assert.Equal(1, row.Id);
        Assert.Equal("Planificada", row.StatusLabel);
        Assert.Equal("warning", row.StatusCss);
        Assert.Equal("/LaboratoryOperations/Requerimientos?planId=1", row.NextUrl);
        Assert.StartsWith("Periodos 1–2 · ", row.TimeText);

        var status = Assert.Single(panel.StatusCounts);
        Assert.Equal("Planificada", status.Label);
        Assert.Equal(1, status.Count);
    }

    [Fact]
    public async Task Semana_FilasConLosDatosDePresentacion()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ing. Marta Loza");
        db.AddPlanning(1, Today, 2, 2, subject: "Química analítica");

        var panel = await Service(db).GetWeekAsync(Today);
        var row = Assert.Single(panel.Days[2].Practices);

        Assert.Equal("Práctica 1", row.PracticeName);
        Assert.Equal("Química analítica", row.SubjectName);
        Assert.Equal("A", row.GroupLabel);                                  // GroupName
        Assert.Equal("LAB-A · Laboratorio A", row.LaboratoryName);
        Assert.Equal("Ing. Marta Loza", row.TeacherName);
    }

    [Fact]
    public async Task Semana_TodosLosLaboratoriosYPriorizaGrupoDeCurso()
    {
        using var db = new TestDb();
        db.Context.Plannings.Add(new Planning
        {
            Id = 2,
            ManagementId = TestDb.ManagementId,
            TeacherPersonId = TestDb.Teacher1,
            PracticeName = "Práctica completa",
            GroupName = "Ignorado",
            CourseGroup = new CourseGroup
            {
                Id = 30,
                Name = "QMC-2A",
                SubjectId = 1,
                TeacherPersonId = TestDb.Teacher1,
                ManagementId = TestDb.ManagementId
            },
            AllLaboratories = true,
            Status = PlanningStatus.Approved,
            RequestDate = Today,
            ScheduledDate = Today.AddHours(9),
            ScheduledEndDate = Today.AddHours(10),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10)
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var panel = await Service(db).GetWeekAsync(Today);
        var row = Assert.Single(panel.Days[2].Practices);

        Assert.Equal("Todos los laboratorios", row.LaboratoryName);
        Assert.Equal("QMC-2A", row.GroupLabel);      // CourseGroup antes que GroupName
        Assert.Equal("Aprobada", row.StatusLabel);
        Assert.Equal("/LaboratoryOperations/L3?id=2", row.NextUrl);
    }

    [Fact]
    public async Task Serie_OchoSemanas_MarcaLaActual()
    {
        using var db = new TestDb();
        db.AddPlanning(1, Monday, 1, 2);                  // semana actual
        db.AddLegacyPlanning(2, Monday.AddDays(6), new TimeSpan(8, 0, 0), new TimeSpan(9, 0, 0)); // domingo de la actual
        db.AddPlanning(3, Monday.AddDays(-14), 1, 1);     // hace 2 semanas
        db.AddPlanning(4, Monday.AddDays(14), 1, 1);      // semana +2
        db.AddLegacyPlanning(5, Monday.AddDays(20), new TimeSpan(8, 0, 0), new TimeSpan(9, 0, 0)); // domingo de la +2
        db.AddPlanning(6, Monday.AddDays(-7), 1, 1, status: PlanningStatus.Cancelled); // no ocupa
        db.AddPlanning(7, Monday.AddDays(-42), 1, 1);     // fuera del rango de 8 semanas

        var series = await Service(db).GetWeeklySeriesAsync(Today);

        Assert.Equal(8, series.Count);
        Assert.Equal(Monday.AddDays(-35), series[0].Start);
        Assert.Equal(Monday, series[5].Start);
        Assert.Equal(Monday.AddDays(14), series[7].Start);
        Assert.All(series, p => Assert.Equal(p.Start.AddDays(6), p.End));

        // Una sola semana actual, en la posición 6 de 8 (índice 5).
        Assert.Equal(1, series.Count(p => p.IsCurrent));
        Assert.Equal(5, series.TakeWhile(p => !p.IsCurrent).Count());

        Assert.Equal(2, series[5].Count);            // lunes + domingo de la actual
        Assert.Equal(1, series[3].Count);            // hace 2 semanas
        Assert.Equal(2, series[7].Count);            // lunes y domingo de la semana +2
        Assert.Equal(0, series[4].Count);            // solo había una cancelada (no ocupa)

        Assert.Equal("9–15 nov", series[5].Label);   // 09–15 nov 2026, mismo mes
        Assert.Equal("16–22 nov", series[6].Label);
        Assert.Equal("2–8 nov", series[4].Label);
    }
}
