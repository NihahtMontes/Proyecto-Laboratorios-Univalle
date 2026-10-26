using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>
/// Grilla semanal de conflictos de material: avisos con marca por periodo, filtro por tipo,
/// panel de detalle con acciones y reducción de cantidad pedida.
/// </summary>
public class ConflictGridServiceTests
{
    private const int ArticleId = 500;
    private const int OtroArticleId = 501;
    private static readonly DateTime Dia = TestDb.Week1Monday;   // lunes 05/10/2026
    private static readonly string SelDia = Dia.ToString("yyyy-MM-dd");
    private const string UrlActual = "/LaboratoryOperations/Conflictos";

    private static readonly ConflictUserRights Todo = new(true, true, true);
    private static readonly ConflictUserRights SinSuperAdmin = new(true, true, false);

    private static ConflictGridService Servicio(TestDb db) => new(
        db.Context,
        new ArticleAvailabilityService(db.Context, new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance)),
        new AcademicManagementService(db.Context));

    private static ConflictResolutionService Resolucion(TestDb db) => new(db.Context, new AcademicManagementService(db.Context));

    /// <summary>Dos prácticas cruzadas en los periodos 1-2 (07:35–09:25) que juntas superan el stock.</summary>
    private static void SembrarCruce(TestDb db, int articuloId, decimal stock, decimal cantidad)
    {
        db.AddArticleWithStock(articuloId, "ART-500", "Matraz 250 ml", stock);
        db.AddPlanning(10, Dia, 1, 2);                                    // 07:35–09:25
        db.AddPlanning(20, Dia, 1, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);
        db.AddRequirement(10, articuloId, cantidad);
        db.AddRequirement(20, articuloId, cantidad);
    }

    [Fact]
    public async Task Overlap_EnPeriodos_MarcaItemYCellsConLaSuma()
    {
        using var db = new TestDb();
        SembrarCruce(db, ArticleId, stock: 4, cantidad: 3);               // piden 6 a la vez y hay 4

        var grilla = await Servicio(db).BuildAsync(
            new ConflictGridQuery(TestDb.ManagementId, 1, null, null, null, null), Todo, UrlActual);

        Assert.NotNull(grilla);
        var item = Assert.Single(grilla!.Items);
        Assert.Equal(ConflictKind.Overlap, item.Kind);
        Assert.Equal("C1", item.Tag);
        Assert.Equal(new[] { 1, 2 }, item.PeriodOrders);

        var lunes = grilla.Days.Single(d => d.Date == Dia);
        Assert.True(lunes.InManagement);
        Assert.False(lunes.IsSuspended);
        Assert.Equal(2, lunes.PracticeCount);
        var celda1 = Assert.Single(lunes.Cells[1]);
        Assert.Equal(6m, celda1.Load);                                    // suma de ambas prácticas
        Assert.Equal(4m, celda1.Available);
        Assert.Equal("C1", celda1.Tag);
        Assert.Equal(6m, Assert.Single(lunes.Cells[2]).Load);
        // Día hábil con prácticas: la reposición queda habilitada para quien puede gestionar.
        Assert.True(Assert.Single(lunes.Actions, a => a.Key == "recover-day").Enabled);
    }

    [Fact]
    public async Task Shortage_PeriodosSoloDeLaPracticaExcedente()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 4);
        db.AddPlanning(10, Dia, 1, 2);                       // 07:35–09:25, pide 10 (sola supera el stock)
        db.AddPlanning(20, Dia, 5, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);   // 11:15–13:05, pide 2
        db.AddRequirement(10, ArticleId, 10);
        db.AddRequirement(20, ArticleId, 2);

        var grilla = await Servicio(db).BuildAsync(
            new ConflictGridQuery(TestDb.ManagementId, 1, null, null, null, null), Todo, UrlActual);

        var item = Assert.Single(grilla!.Items);
        Assert.Equal(ConflictKind.Shortage, item.Kind);
        Assert.Equal(new[] { 1, 2 }, item.PeriodOrders);   // los periodos 5-6 de PRA-00020 quedan fuera
        Assert.Equal(new[] { 10 }, item.InvolvedPracticeIds);
    }

    [Fact]
    public async Task FiltroTipo_Rotation_MantieneCountsYFiltraItems()
    {
        using var db = new TestDb();
        // Rotación: franjas consecutivas del mismo artículo (total 6 > stock 4, sin cruce).
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 4);
        db.AddPlanning(10, Dia, 1, 2);
        db.AddPlanning(20, Dia, 3, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);
        db.AddRequirement(10, ArticleId, 3);
        db.AddRequirement(20, ArticleId, 3);
        // Solapamiento de otro artículo en los mismos periodos (su Tag global sigue siendo "C1").
        db.AddArticleWithStock(OtroArticleId, "ART-501", "Tubo de ensayo", stock: 4);
        db.AddPlanning(30, Dia, 1, 2);
        db.AddPlanning(40, Dia, 1, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);
        db.AddRequirement(30, OtroArticleId, 3);
        db.AddRequirement(40, OtroArticleId, 3);

        var grilla = await Servicio(db).BuildAsync(
            new ConflictGridQuery(TestDb.ManagementId, 1, null, null, null, ConflictKind.Rotation), Todo, UrlActual);

        Assert.NotNull(grilla);
        Assert.Equal(1, grilla!.Counts[ConflictKind.Overlap]);            // Counts sobre todos los avisos
        Assert.Equal(1, grilla.Counts[ConflictKind.Rotation]);
        var item = Assert.Single(grilla.Items);
        Assert.Equal(ArticleId, item.ArticleId);
        Assert.Equal("C2", item.Tag);                                     // el solapamiento (C1) va primero por gravedad
        Assert.Equal(ConflictKind.Rotation,
            Assert.Single(grilla.Days.Single(d => d.Date == Dia).Cells[1]).Kind);
    }

    [Fact]
    public async Task Panel_ArmaLoadsPracticasAccionesYReducible()
    {
        using var db = new TestDb();
        SembrarCruce(db, ArticleId, stock: 4, cantidad: 3);

        var grilla = await Servicio(db).BuildAsync(
            new ConflictGridQuery(TestDb.ManagementId, 1, null, SelDia, ArticleId, null), SinSuperAdmin, UrlActual);

        Assert.NotNull(grilla);
        var panel = grilla!.Selected;
        Assert.NotNull(panel);
        Assert.Equal("C1", panel!.Item.Tag);
        Assert.Equal(ConflictKind.Overlap, panel.Type.Kind);

        // Cargas: periodos 1 y 2 con la suma (6) que supera el stock (4).
        Assert.Equal(new[] { 1, 2 }, panel.Loads.Select(l => l.Order));
        Assert.All(panel.Loads, l => Assert.Equal(6m, l.Load));
        Assert.All(panel.Loads, l => Assert.True(l.Over));

        // Prácticas: ambas involucradas, en sus periodos, con cantidad reducible (una línea Planificada en gestión activa).
        Assert.Equal(new[] { 10, 20 }, panel.Practices.Select(p => p.Practice.Id));
        Assert.All(panel.Practices, p => Assert.True(p.Involved));
        Assert.All(panel.Practices, p => Assert.Equal(new[] { 1, 2 }, p.PeriodOrders));
        Assert.All(panel.Practices, p => Assert.Equal(3m, p.ReducibleQuantity));

        // Acciones por práctica, siempre en este orden; reprogramar exige SuperAdmin.
        var acciones = panel.Practices[0].Actions;
        Assert.Equal(new[] { "reschedule", "reduce", "materials", "grid" }, acciones.Select(a => a.Key));
        var reprogramar = acciones[0];
        Assert.False(reprogramar.Enabled);
        Assert.Equal("Solo SuperAdmin puede reprogramar", reprogramar.DisabledReason);
        Assert.Contains("reprogramar=10", reprogramar.Url);
        Assert.Contains($"returnUrl={Uri.EscapeDataString(UrlActual)}", reprogramar.Url);
        Assert.True(acciones[1].Enabled);                                 // reducir: Planificada con una sola línea
        Assert.Null(acciones[1].Url);                                     // acción de cliente: modal de la página
        Assert.True(acciones[2].Enabled);
        Assert.Contains($"/LaboratoryOperations/Requerimientos?planId=10&returnUrl={Uri.EscapeDataString(UrlActual)}", acciones[2].Url);
        Assert.True(acciones[3].Enabled);
        Assert.Contains("resaltar=10", acciones[3].Url);

        // Acción del conflicto: existencias de inventario → pantalla de existencias (admin) con retorno.
        var stock = Assert.Single(panel.Actions, a => a.Key == "stock");
        Assert.True(stock.Enabled);
        Assert.StartsWith("/Catalogs/Stock/Edit/1/500?returnUrl=", stock.Url);
    }

    [Fact]
    public async Task ReduceQuantityAsync_ReducePersisteYValida()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 4);
        db.AddPlanning(10, Dia, 1, 2);
        db.AddRequirement(10, ArticleId, 10);
        var servicio = Resolucion(db);

        // Rechaza una cantidad mayor o igual a la actual.
        var rechazo = await servicio.ReduceQuantityAsync(10, ArticleId, 10m);
        Assert.False(rechazo.Ok);
        Assert.Contains("menor a la actual", rechazo.Message);

        // Reduce y persiste cantidad y unidades esperadas.
        var ok = await servicio.ReduceQuantityAsync(10, ArticleId, 6.5m);
        Assert.True(ok.Ok);
        Assert.Contains("PRA-00010", ok.Message);
        Assert.Contains("reducida de 10 a 6,5", ok.Message);
        var linea = db.Context.PlanningRequirements.AsNoTracking()
            .Single(d => d.PlanningId == 10 && d.ArticleId == ArticleId);
        Assert.Equal(6.5m, linea.RequestedQuantity);
        Assert.Equal(7, linea.ExpectedQuantity);

        // Rechaza en estado Aprobada.
        var plan = db.Context.Plannings.AsTracking().Single(p => p.Id == 10);
        plan.Status = PlanningStatus.Approved;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        var rechazada = await servicio.ReduceQuantityAsync(10, ArticleId, 3m);
        Assert.False(rechazada.Ok);
        Assert.Contains("aprobada", rechazada.Message);
    }

    [Fact]
    public async Task GetPlanningTagsAsync_Overlap_MismaTagQueBuildAsyncYRelacionadas()
    {
        using var db = new TestDb();
        SembrarCruce(db, ArticleId, stock: 4, cantidad: 3);               // prácticas 10 y 20 cruzadas

        var servicio = Servicio(db);
        var grilla = await servicio.BuildAsync(
            new ConflictGridQuery(TestDb.ManagementId, 1, null, null, null, null), Todo, UrlActual);
        var tags = await servicio.GetPlanningTagsAsync([TestDb.Week1Monday]);

        Assert.NotNull(grilla);
        var item = Assert.Single(grilla!.Items);
        Assert.Equal("C1", item.Tag);

        var tag10 = Assert.Single(tags[10]);
        var tag20 = Assert.Single(tags[20]);
        // Misma etiqueta que la grilla de conflictos de la semana.
        Assert.Equal(item.Key, tag10.Key);
        Assert.Equal(item.Tag, tag10.Tag);
        Assert.Equal(item.Tag, tag20.Tag);
        Assert.Equal(ConflictKind.Overlap, tag10.Kind);
        Assert.Equal(item.Date, tag10.Date);
        Assert.Equal(item.ArticleName, tag10.ArticleName);
        // Ambas involucradas y cada una ve a la otra como relacionada.
        Assert.True(tag10.Involved);
        Assert.True(tag20.Involved);
        Assert.Equal(new[] { 20 }, tag10.RelatedPlanningIds);
        Assert.Equal(new[] { 10 }, tag20.RelatedPlanningIds);
        // Url abre el conflicto en el módulo de Conflictos.
        Assert.Equal($"/LaboratoryOperations/Conflictos?selDate={SelDia}&articleId={ArticleId}", tag10.Url);
    }

    [Fact]
    public async Task GetPlanningTagsAsync_PracticaQueNoCruza_TieneEtiquetaInvolvedFalse()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 4);
        db.AddPlanning(10, Dia, 1, 2);                                    // pide 10: sola supera el stock (Shortage)
        db.AddPlanning(20, Dia, 5, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);   // 11:15–13:05, no cruza
        db.AddRequirement(10, ArticleId, 10);
        db.AddRequirement(20, ArticleId, 2);

        var tags = await Servicio(db).GetPlanningTagsAsync([TestDb.Week1Monday]);

        var tag10 = Assert.Single(tags[10]);
        Assert.Equal(ConflictKind.Shortage, tag10.Kind);
        Assert.True(tag10.Involved);
        // La que no se cruza lleva la misma etiqueta del conflicto, pero sin estar involucrada.
        var tag20 = Assert.Single(tags[20]);
        Assert.Equal(tag10.Key, tag20.Key);
        Assert.Equal(tag10.Tag, tag20.Tag);
        Assert.False(tag20.Involved);
        Assert.Equal(new[] { 10 }, tag20.RelatedPlanningIds);
    }

    [Fact]
    public async Task GetPlanningTagsAsync_FechasDeLaMismaSemana_NoDuplica()
    {
        using var db = new TestDb();
        SembrarCruce(db, ArticleId, stock: 4, cantidad: 3);               // conflicto único el lunes

        // Lunes y miércoles de la misma semana: una sola etiqueta por conflicto y planificación.
        var tags = await Servicio(db).GetPlanningTagsAsync(
            [TestDb.Week1Monday, TestDb.Week1Monday.AddDays(2)]);

        Assert.Equal(2, tags.Count);
        Assert.Single(tags[10]);
        Assert.Single(tags[20]);
    }
}
