using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>Análisis de conflictos de material por artículo y día: tramos de demanda, tipo de conflicto y sugerencias.</summary>
public class ConflictAnalysisTests
{
    private const int ArticleId = 500;
    private const int SinDemandaId = 501;

    private static readonly DateTime Dia = TestDb.Week1Monday;   // lunes 05/10/2026

    private static TimeSpan Hora(int horas) => new(horas, 0, 0);

    private static ArticleAvailabilityService Servicio(TestDb db) =>
        new(db.Context, new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance));

    // ==================== Tramos de demanda (DemandPeak.Segments) ====================

    [Fact]
    public void Segments_Adjacentes_NoCompartenTramo()
    {
        var tramos = DemandPeak.Segments(
        [
            (1, new TimedDemand(3m, Hora(8), Hora(9))),
            (2, new TimedDemand(3m, Hora(9), Hora(10)))     // termina cuando la otra empieza: no se cruzan
        ]);

        Assert.Equal(2, tramos.Count);
        Assert.All(tramos, t => Assert.False(t.ActiveIds.Contains(1) && t.ActiveIds.Contains(2)));
        Assert.Equal(3m, tramos[0].Load);
    }

    [Fact]
    public void Segments_Solapadas_TramoConAmbasYSuma()
    {
        var tramos = DemandPeak.Segments(
        [
            (1, new TimedDemand(3m, Hora(9), Hora(11))),
            (2, new TimedDemand(4m, Hora(10), Hora(12)))
        ]);

        var ambas = Assert.Single(tramos, t => t.ActiveIds.Count == 2);
        Assert.Equal(7m, ambas.Load);
        Assert.Equal(Hora(10), ambas.Start);
        Assert.Equal(Hora(11), ambas.End);
    }

    [Fact]
    public void Segments_DiaCompleto_ActivaEnTodosLosTramos()
    {
        var tramos = DemandPeak.Segments(
        [
            (1, new TimedDemand(2m, null, null)),           // sin horario: ocupa todo el día
            (2, new TimedDemand(3m, Hora(8), Hora(9)))
        ]);

        Assert.Equal(3, tramos.Count);
        Assert.All(tramos, t => Assert.Contains(1, t.ActiveIds));
        Assert.Equal(TimeSpan.Zero, tramos[0].Start);
        Assert.Equal(TimeSpan.FromDays(1), tramos[^1].End);
        var cruce = Assert.Single(tramos, t => t.ActiveIds.Contains(2));
        Assert.Equal(5m, cruce.Load);
    }

    // ==================== Tipo de conflicto por día (servicio) ====================

    [Fact]
    public async Task Shortage_SoloInvolucraLaPracticaQueExcede()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 4);
        db.AddPlanning(10, Dia, 1, 2);                       // 07:35–09:25, pide 10
        db.AddPlanning(20, Dia, 5, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);   // 11:15–13:05, pide 2
        db.AddRequirement(10, ArticleId, 10);
        db.AddRequirement(20, ArticleId, 2);

        var semana = await Servicio(db).GetRangeAsync(Dia, Dia);
        var demanda = Assert.Single(semana.Demands, d => d.ArticleId == ArticleId);

        Assert.Equal(ConflictKind.Shortage, demanda.Kind);
        Assert.Equal(new[] { 10 }, demanda.InvolvedPracticeIds);   // PRA-00020 no se cruza: queda fuera
        Assert.Equal(10m, demanda.PeakRequested);
        Assert.Equal(6m, demanda.Shortage);
    }

    [Fact]
    public async Task Overlap_MarcaTodasLasPracticasDelCruce()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 4);
        db.AddPlanning(10, Dia, 3, 2);                       // 09:25–11:15, pide 3
        db.AddPlanning(20, Dia, 4, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);   // 10:25–12:05, pide 3
        db.AddRequirement(10, ArticleId, 3);
        db.AddRequirement(20, ArticleId, 3);

        var semana = await Servicio(db).GetRangeAsync(Dia, Dia);
        var demanda = Assert.Single(semana.Demands, d => d.ArticleId == ArticleId);

        Assert.Equal(ConflictKind.Overlap, demanda.Kind);
        Assert.Equal(new[] { 10, 20 }, demanda.InvolvedPracticeIds);
        Assert.Equal(2m, demanda.Shortage);                  // piden 6 a la vez y hay 4
    }

    [Fact]
    public async Task Rotation_Adjacentes_RequiereRotacion()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 4);
        db.AddPlanning(10, Dia, 1, 2);                       // 07:35–09:25, pide 3
        db.AddPlanning(20, Dia, 3, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);   // 09:25–11:15 (adyacente), pide 3
        db.AddRequirement(10, ArticleId, 3);
        db.AddRequirement(20, ArticleId, 3);

        var semana = await Servicio(db).GetRangeAsync(Dia, Dia);
        var demanda = Assert.Single(semana.Demands, d => d.ArticleId == ArticleId);

        Assert.Equal(ConflictKind.Rotation, demanda.Kind);
        Assert.True(demanda.IsNotice);
        Assert.Empty(demanda.OverSegments);

        var consejo = ConflictAdvisor.Advise(demanda, semana.Practices);
        Assert.Equal("Requiere rotación", consejo.Title);
        Assert.Contains("PRA-00010 07:35–09:25 → PRA-00020 09:25–11:15", Assert.Single(consejo.Actions));
    }

    [Fact]
    public async Task NoStock_EsAvisoSinEvaluar()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Reactivo sin stock", stock: null);
        db.AddPlanning(10, Dia, 1, 2);
        db.AddRequirement(10, ArticleId, 5);

        var semana = await Servicio(db).GetRangeAsync(Dia, Dia);
        var demanda = Assert.Single(semana.Demands, d => d.ArticleId == ArticleId);

        Assert.Equal(ConflictKind.NoStock, demanda.Kind);
        Assert.True(demanda.IsNotice);
        Assert.Equal(1, semana.NoStockCount);
        Assert.Contains(demanda, semana.Notices);
        Assert.Equal(new[] { 10 }, demanda.InvolvedPracticeIds);   // sin stock: todas las prácticas del día
    }

    // ==================== Sugerencias (ConflictAdvisor) ====================

    [Fact]
    public async Task Advisor_Overlap_SugiereLaMenorQueAlcanza()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 6);
        db.AddPlanning(10, Dia, 3, 3);                                        // 09:25–12:05, pide 5
        db.AddPlanning(20, Dia, 4, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);   // 10:25–12:05, pide 2
        db.AddPlanning(30, Dia, 4, 3, laboratoryId: TestDb.LabB);             // 10:25–13:05, pide 1
        db.AddRequirement(10, ArticleId, 5);
        db.AddRequirement(20, ArticleId, 2);
        db.AddRequirement(30, ArticleId, 1);

        var semana = await Servicio(db).GetRangeAsync(Dia, Dia);
        var demanda = Assert.Single(semana.Demands, d => d.ArticleId == ArticleId);

        var consejo = ConflictAdvisor.Advise(demanda, semana.Practices);

        Assert.Equal(ConflictKind.Overlap, consejo.Kind);
        Assert.Equal("Conflicto de horario", consejo.Title);
        // En el pico piden 8 y hay 6: mover PRA-00020 (2) basta y es la menor que alcanza.
        Assert.Equal(new[] { 20 }, consejo.SuggestedMoveIds);
        Assert.Equal(2m, consejo.StockIncrease);
        Assert.Contains(consejo.Actions, a => a.StartsWith("Mueve PRA-00020 (2)"));
    }

    [Fact]
    public async Task Advisor_Greedy_CubreElFaltanteConVarias()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 4);
        db.AddPlanning(10, Dia, 3, 2);                                        // 09:25–11:15, pide 3
        db.AddPlanning(20, Dia, 4, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);   // 10:25–12:05, pide 3
        db.AddPlanning(30, Dia, 3, 3, laboratoryId: TestDb.LabB);             // 09:25–12:05, pide 3
        db.AddRequirement(10, ArticleId, 3);
        db.AddRequirement(20, ArticleId, 3);
        db.AddRequirement(30, ArticleId, 3);

        var semana = await Servicio(db).GetRangeAsync(Dia, Dia);
        var demanda = Assert.Single(semana.Demands, d => d.ArticleId == ArticleId);

        var consejo = ConflictAdvisor.Advise(demanda, semana.Practices);

        Assert.Equal(ConflictKind.Overlap, consejo.Kind);
        // En el pico piden 9 y hay 4: faltan 5 y ninguna práctica sola alcanza (3 < 5).
        Assert.Equal(2, consejo.SuggestedMoveIds.Count);
        Assert.Equal(new[] { 10, 20 }, consejo.SuggestedMoveIds);
    }

    // ==================== Detalle de un día (GetArticleDayAsync) ====================

    [Fact]
    public async Task GetArticleDayAsync_DevuelvePracticasOrdenadasYNuloSinDemanda()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 10);
        db.AddArticleWithStock(SinDemandaId, "ART-501", "Artículo sin uso", stock: 5);
        db.AddPlanning(20, Dia, 5, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);   // 11:15–13:05
        db.AddPlanning(10, Dia, 1, 2);                       // 07:35–09:25
        db.AddRequirement(10, ArticleId, 4);
        db.AddRequirement(20, ArticleId, 2);

        var servicio = Servicio(db);

        var detalle = await servicio.GetArticleDayAsync(Dia, ArticleId);

        Assert.NotNull(detalle);
        Assert.Equal(ArticleId, detalle.Demand.ArticleId);
        Assert.Equal(new[] { 10, 20 }, detalle.Practices.Select(p => p.Id));   // por hora de inicio, no por Id
        Assert.Null(detalle.EquipmentId);

        Assert.Null(await servicio.GetArticleDayAsync(Dia, SinDemandaId));
    }
}
