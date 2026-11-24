using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using OfficeOpenXml;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>
/// Reporte «Cambios de material del docente»: compara lo que cada docente pidió en su solicitud
/// (planificación con review «Enviada») con el kit escalado de la práctica, sin contarle al docente
/// los ajustes que la Inge hizo al aprobar con cambios. Cubre la clasificación (Aumentó, Disminuyó,
/// Agregó, Quitó), los filtros por fecha local de la primera «Enviada» y por docente, el resumen por
/// docente y el Excel de dos hojas (también sin filas). Base EF InMemory (TestDb); no se toca SQL Server.
/// </summary>
public class MaterialChangesReportTests
{
    static MaterialChangesReportTests() => ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    /// <summary>IWebHostEnvironment mínimo: el reporte no usa plantillas, pero el constructor lo pide.</summary>
    private sealed class WebRootVacio : IWebHostEnvironment
    {
        public WebRootVacio() => WebRootPath = AppContext.BaseDirectory;
        public string ApplicationName { get; set; } = "Laboratorios.Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static ReportService Servicio(TestDb db) =>
        new(db.Context, new WebRootVacio(), NullLogger<ReportService>.Instance);

    // ==================== Siembra (helpers privados del test; TestDb.cs no se toca) ====================

    private static void SembrarArticulo(TestDb db, int id, string code, string name,
        string? categoria = null, ArticleCategory? categoriaNav = null)
    {
        db.Context.Articles.Add(new Article
        {
            Id = id, Code = code, Name = name,
            Category = categoria,
            ArticleCategoryId = categoriaNav?.Id,
            ArticleCategory = categoriaNav
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static void SembrarKit(TestDb db, int practiceId, int articleId, decimal? cantidad, bool escalable)
    {
        db.Context.PracticeRequirements.Add(new PracticeRequirement
        {
            PracticeId = practiceId, ArticleId = articleId, Quantity = cantidad, IsScalable = escalable
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static void SembrarGrupo(TestDb db, int id, string nombre, int subjectId, int? estudiantes, int? factor)
    {
        db.Context.CourseGroups.Add(new CourseGroup
        {
            Id = id, Code = $"G-{id}", Name = nombre, SubjectId = subjectId,
            StudentCount = estudiantes, ScalingFactor = factor, ManagementId = TestDb.ManagementId
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    /// <summary>Solicitud docente con programación (schedule → práctica del kit) y materia propia.</summary>
    private static void SembrarSolicitud(TestDb db, int id, int docenteId, DateTime fechaUtcPrimeraEnviada,
        int subjectId, string materia, int practiceId, int scheduleId, int? courseGroupId = null,
        int? students = null, string grupo = "A", string status = PlanningStatus.Requested,
        bool borrada = false, string? observacion = null, DateTime? fechaPractica = null)
    {
        db.Context.Plannings.Add(new Planning
        {
            Id = id,
            ManagementId = TestDb.ManagementId,
            TeacherPersonId = docenteId,
            PracticeName = $"Práctica {id}",
            GroupName = grupo,
            Status = status,
            RequestDate = fechaUtcPrimeraEnviada,
            ScheduledDate = fechaPractica ?? fechaUtcPrimeraEnviada,
            StudentCount = students,
            CourseGroupId = courseGroupId,
            PracticeScheduleId = scheduleId,
            PracticeSchedule = new PracticeSchedule { Id = scheduleId, Code = $"PP-{scheduleId}", PracticeId = practiceId },
            Subject = new Subject { Id = subjectId, Name = materia },
            Observations = observacion,
            IsDeleted = borrada
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static void SembrarReview(TestDb db, int id, int planningId, PlanningReviewAction action,
        DateTime utc, params (int ArticuloId, decimal? Previo, decimal? Nueva)[] lineas)
    {
        var review = new PlanningReview
        {
            Id = id, PlanningId = planningId, Action = action,
            FromStatus = "Solicitada", ToStatus = "Aprobada", UserId = 1, CreatedDate = utc
        };
        foreach (var linea in lineas)
            review.Lines.Add(new PlanningReviewLine
            {
                ArticleId = linea.ArticuloId, PreviousQuantity = linea.Previo, NewQuantity = linea.Nueva
            });
        db.Context.PlanningReviews.Add(review);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    /// <summary>Gestión con un grupo (20 estudiantes, factor 4), práctica 50 con kit de 6 artículos
    /// y una solicitud de Ana Torres con los pedidos del caso (a).</summary>
    private static TestDb SembrarCasoPrincipal()
    {
        var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        SembrarGrupo(db, 1, "A", subjectId: 500, estudiantes: 30, factor: 4);
        db.Context.Practices.Add(new Practice { Id = 50, Code = "P-50", Name = "Titulación ácido-base", SubjectId = 500 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        // Kit de la práctica 50: escalable 1→5, fijo 10, fuera de kit, kit sin pedido, kit null y escalable igualado.
        SembrarKit(db, 50, articleId: 1, cantidad: 1, escalable: true);    // COD-1: 1 × 20 ÷ 4 = 5
        SembrarKit(db, 50, articleId: 2, cantidad: 10, escalable: false);  // COD-2
        SembrarKit(db, 50, articleId: 4, cantidad: 5, escalable: false);   // COD-4
        SembrarKit(db, 50, articleId: 5, cantidad: null, escalable: false);// COD-5: null cuenta 1
        SembrarKit(db, 50, articleId: 6, cantidad: 2, escalable: true);    // COD-6: 2 × 20 ÷ 4 = 10

        SembrarArticulo(db, 1, "COD-1", "Reactivo X", categoria: "Químicos");
        SembrarArticulo(db, 2, "COD-2", "Vaso precipitado");
        SembrarArticulo(db, 3, "COD-3", "Gafas de seguridad");
        SembrarArticulo(db, 4, "COD-4", "Guantes de nitrilo");
        SembrarArticulo(db, 5, "COD-5", "Cuchara de madera");
        SembrarArticulo(db, 6, "COD-6", "Pipeta graduada");

        SembrarSolicitud(db, 100, TestDb.Teacher1, new DateTime(2026, 11, 3, 15, 0, 0),
            subjectId: 500, materia: "Química Analítica", practiceId: 50, scheduleId: 1000,
            courseGroupId: 1, students: 20, observacion: "Con exceso de reactivo",
            fechaPractica: new DateTime(2026, 11, 5));
        SembrarReview(db, 2001, 100, PlanningReviewAction.Submitted, new DateTime(2026, 11, 3, 15, 0, 0));

        // Pedidos del docente (sin líneas de review: la Inge no ajustó nada).
        db.AddRequirement(100, 1, 7);   // kit 5 → Aumentó +2
        db.AddRequirement(100, 2, 6);   // kit 10 → Disminuyó −4
        db.AddRequirement(100, 3, 2);   // fuera del kit → Agregó +2
        db.AddRequirement(100, 5, 1);   // kit null=1 → igual, fuera del detalle
        db.AddRequirement(100, 6, 10);  // kit escalado 10 → igual, fuera del detalle
        // COD-4 sin pedido → Quitó −5
        return db;
    }

    // ==================== (a) Kit escalado contra el pedido ====================

    [Fact]
    public async Task Detalle_clasifica_kit_escalado_contra_lo_pedido()
    {
        using var db = SembrarCasoPrincipal();

        var reporte = await Servicio(db).GetMaterialChangesAsync(TestDb.ManagementId);

        Assert.Equal(4, reporte.Rows.Count);   // COD-5 y COD-6 quedan iguales: no van al detalle

        // Orden del detalle por artículo: COD-1, COD-2, COD-3, COD-4.
        var escalado = reporte.Rows[0];
        Assert.Equal("COD-1 · Reactivo X", escalado.Articulo);
        Assert.Equal("Químicos", escalado.Categoria);            // ArticleCategory null → Category legada
        Assert.Equal(5m, escalado.Kit);                          // 1 × 20 estudiantes ÷ 4 = 5
        Assert.Equal(7m, escalado.Pedido);
        Assert.Equal(2m, escalado.Diferencia);
        Assert.Equal("Aumentó", escalado.Cambio);
        Assert.Equal("Ana Torres", escalado.Docente);
        Assert.Equal("Química Analítica", escalado.Materia);
        Assert.Equal("A", escalado.Grupo);
        Assert.Equal("Práctica 100", escalado.Practica);
        Assert.Equal(PlanningStatus.Requested, escalado.Estado);
        Assert.Equal("Con exceso de reactivo", escalado.Observacion);
        Assert.Equal(new DateTime(2026, 11, 5), escalado.FechaPractica);
        Assert.Equal(BoliviaTime.ToLocal(new DateTime(2026, 11, 3, 15, 0, 0)).Date, escalado.FechaSolicitud);

        var disminuido = reporte.Rows[1];
        Assert.Equal("COD-2 · Vaso precipitado", disminuido.Articulo);
        Assert.Equal("Disminuyó", disminuido.Cambio);
        Assert.Equal(10m, disminuido.Kit);
        Assert.Equal(6m, disminuido.Pedido);
        Assert.Equal(-4m, disminuido.Diferencia);

        var agregado = reporte.Rows[2];
        Assert.Equal("COD-3 · Gafas de seguridad", agregado.Articulo);
        Assert.Equal("Agregó", agregado.Cambio);
        Assert.Equal(0m, agregado.Kit);
        Assert.Equal(2m, agregado.Pedido);

        var quitado = reporte.Rows[3];
        Assert.Equal("COD-4 · Guantes de nitrilo", quitado.Articulo);
        Assert.Equal("Quitó", quitado.Cambio);
        Assert.Equal(5m, quitado.Kit);
        Assert.Equal(0m, quitado.Pedido);

        Assert.DoesNotContain(reporte.Rows, f => f.Articulo.Contains("COD-5"));
        Assert.DoesNotContain(reporte.Rows, f => f.Articulo.Contains("COD-6"));

        // Resumen de la única docente con solicitudes en el rango.
        var resumen = Assert.Single(reporte.Summaries);
        Assert.Equal("Ana Torres", resumen.Docente);
        Assert.Equal(1, resumen.Solicitudes);
        Assert.Equal(1, resumen.ConCambios);
        Assert.Equal(4, resumen.ArticulosCambiados);
        Assert.Equal(1, resumen.Aumentos);
        Assert.Equal(1, resumen.Disminuciones);
        Assert.Equal(1, resumen.Agregados);
        Assert.Equal(1, resumen.Quitados);
        Assert.Equal(0, resumen.Reenvios);
    }

    // ==================== (b) Ajuste de la Inge al aprobar con cambios ====================

    [Fact]
    public async Task Ajuste_de_la_inge_cuenta_lo_que_pidio_el_docente_no_lo_que_dejo()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.Context.ArticleCategories.Add(new ArticleCategory { Id = 9, Code = "VID", Name = "Material de vidrio" });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        db.Context.Articles.Add(new Article { Id = 7, Code = "COD-7", Name = "Probeta", ArticleCategoryId = 9 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        db.Context.ChangeTracker.Clear();
        db.Context.Practices.Add(new Practice { Id = 51, Code = "P-51", Name = "Densidad", SubjectId = 501 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        SembrarKit(db, 51, 7, cantidad: 2, escalable: false);

        SembrarSolicitud(db, 110, TestDb.Teacher1, new DateTime(2026, 11, 4, 14, 0, 0),
            subjectId: 501, materia: "Física", practiceId: 51, scheduleId: 1010);
        SembrarReview(db, 2101, 110, PlanningReviewAction.Submitted, new DateTime(2026, 11, 4, 14, 0, 0));
        // Al aprobar con cambios la Inge dejó 5, pero el docente pidió 3 (requirements reescritos a 5).
        db.AddRequirement(110, 7, 5);
        SembrarReview(db, 2102, 110, PlanningReviewAction.ApprovedWithChanges, new DateTime(2026, 11, 4, 16, 0, 0),
            (7, 3m, 5m));

        var reporte = await Servicio(db).GetMaterialChangesAsync(TestDb.ManagementId);

        var fila = Assert.Single(reporte.Rows);
        Assert.Equal("COD-7 · Probeta", fila.Articulo);
        Assert.Equal("Aumentó", fila.Cambio);
        Assert.Equal(2m, fila.Kit);
        Assert.Equal(3m, fila.Pedido);       // lo que pidió el docente, no los 5 de la Inge
        Assert.Equal(1m, fila.Diferencia);
        Assert.Equal("Material de vidrio", fila.Categoria);   // ArticleCategory.Name sobre Category legada
    }

    // ==================== (c) Artículos agregados por la Inge (Previous null) ====================

    [Fact]
    public async Task Articulo_agregado_por_la_inge_no_es_pedido_del_docente()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.Context.Practices.Add(new Practice { Id = 52, Code = "P-52", Name = "Mezclas", SubjectId = 502 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        SembrarKit(db, 52, 9, cantidad: 4, escalable: false);   // kit con artículo que solo agregó la Inge

        SembrarArticulo(db, 8, "COD-8", "Balanza granataria");
        SembrarArticulo(db, 9, "COD-9", "Embudo de vidrio");

        SembrarSolicitud(db, 120, TestDb.Teacher1, new DateTime(2026, 11, 6, 14, 0, 0),
            subjectId: 502, materia: "Química", practiceId: 52, scheduleId: 1020);
        SembrarReview(db, 2201, 120, PlanningReviewAction.Submitted, new DateTime(2026, 11, 6, 14, 0, 0));
        // La Inge agregó el COD-8 fuera del kit (Previous null) y dejó el COD-9 del kit sin pedido del docente.
        db.AddRequirement(120, 8, 4);
        SembrarReview(db, 2202, 120, PlanningReviewAction.ApprovedWithChanges, new DateTime(2026, 11, 6, 16, 0, 0),
            (8, null, 4m), (9, null, 2m));

        var reporte = await Servicio(db).GetMaterialChangesAsync(TestDb.ManagementId);

        var quitado = Assert.Single(reporte.Rows);
        Assert.Equal("COD-9 · Embudo de vidrio", quitado.Articulo);
        Assert.Equal("Quitó", quitado.Cambio);
        Assert.Equal(4m, quitado.Kit);
        Assert.Equal(0m, quitado.Pedido);    // Previous null: el docente nunca lo pidió
        // COD-8 (fuera del kit, solo de la Inge): pedido 0 y kit 0 → sin fila.
        Assert.DoesNotContain(reporte.Rows, f => f.Articulo.Contains("COD-8"));
    }

    // ==================== (d) Borradas y sin «Enviada» no entran ====================

    [Fact]
    public async Task No_entran_borradas_ni_planificaciones_sin_enviada()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.Context.Practices.Add(new Practice { Id = 60, Code = "P-60", Name = "Sin kit", SubjectId = 510 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        // Borrada con «Enviada»: fuera.
        SembrarSolicitud(db, 130, TestDb.Teacher1, new DateTime(2026, 11, 10, 14, 0, 0),
            subjectId: 510, materia: "Química", practiceId: 60, scheduleId: 1030, borrada: true);
        SembrarReview(db, 2301, 130, PlanningReviewAction.Submitted, new DateTime(2026, 11, 10, 14, 0, 0));
        // Planificada por la Inge sin historial: fuera.
        SembrarSolicitud(db, 140, TestDb.Teacher1, new DateTime(2026, 11, 11, 14, 0, 0),
            subjectId: 511, materia: "Química", practiceId: 60, scheduleId: 1040, status: PlanningStatus.Approved);
        // Con review del revisor pero sin «Enviada»: fuera.
        SembrarSolicitud(db, 150, TestDb.Teacher1, new DateTime(2026, 11, 12, 14, 0, 0),
            subjectId: 512, materia: "Química", practiceId: 60, scheduleId: 1050, status: PlanningStatus.Approved);
        SembrarReview(db, 2302, 150, PlanningReviewAction.Approved, new DateTime(2026, 11, 12, 16, 0, 0));
        // Solicitud legítima del docente: única que entra (sin cambios de material).
        SembrarSolicitud(db, 160, TestDb.Teacher1, new DateTime(2026, 11, 13, 14, 0, 0),
            subjectId: 513, materia: "Química", practiceId: 60, scheduleId: 1060);
        SembrarReview(db, 2303, 160, PlanningReviewAction.Submitted, new DateTime(2026, 11, 13, 14, 0, 0));

        var reporte = await Servicio(db).GetMaterialChangesAsync(TestDb.ManagementId);

        Assert.Empty(reporte.Rows);
        var resumen = Assert.Single(reporte.Summaries);
        Assert.Equal(1, resumen.Solicitudes);
        Assert.Equal(0, resumen.ConCambios);
    }

    // ==================== (e) Filtros desde/hasta (fecha local de la primera «Enviada») y docente ====================

    [Fact]
    public async Task Filtros_de_fecha_local_y_docente()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.AddIntern(TestDb.Teacher2, "Beto Vargas");
        db.Context.Practices.Add(new Practice { Id = 61, Code = "P-61", Name = "Sin kit", SubjectId = 511 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        // Enviada UTC 01/11 03:00 → local 31/10 23:00 (borde del día por UTC−4): su fecha local es 31/10.
        SembrarSolicitud(db, 170, TestDb.Teacher1, new DateTime(2026, 11, 1, 3, 0, 0),
            subjectId: 511, materia: "Química", practiceId: 61, scheduleId: 1070);
        SembrarReview(db, 2401, 170, PlanningReviewAction.Submitted, new DateTime(2026, 11, 1, 3, 0, 0));
        // Enviada UTC 02/11 15:00 → local 02/11 11:00.
        SembrarSolicitud(db, 175, TestDb.Teacher2, new DateTime(2026, 11, 2, 15, 0, 0),
            subjectId: 512, materia: "Química", practiceId: 61, scheduleId: 1071);
        SembrarReview(db, 2402, 175, PlanningReviewAction.Submitted, new DateTime(2026, 11, 2, 15, 0, 0));

        var servicio = Servicio(db);

        // Sin rango: las dos solicitudes.
        var todo = await servicio.GetMaterialChangesAsync(TestDb.ManagementId);
        Assert.Equal(2, todo.Summaries.Count);

        // Desde 01/11: la del borde (local 31/10) queda fuera.
        var desdeNoviembre = await servicio.GetMaterialChangesAsync(TestDb.ManagementId,
            from: new DateTime(2026, 11, 1));
        var resumenDesde = Assert.Single(desdeNoviembre.Summaries);
        Assert.Equal("Beto Vargas", resumenDesde.Docente);

        // Hasta 31/10: solo la del borde (inclusive por día).
        var hastaOctubre = await servicio.GetMaterialChangesAsync(TestDb.ManagementId,
            to: new DateTime(2026, 10, 31));
        var resumenHasta = Assert.Single(hastaOctubre.Summaries);
        Assert.Equal("Ana Torres", resumenHasta.Docente);

        // Rango exacto del día del borde: inclusive.
        var diaDelBorde = await servicio.GetMaterialChangesAsync(TestDb.ManagementId,
            from: new DateTime(2026, 10, 31), to: new DateTime(2026, 10, 31));
        Assert.Equal("Ana Torres", Assert.Single(diaDelBorde.Summaries).Docente);

        // Filtro por docente.
        var soloBeto = await servicio.GetMaterialChangesAsync(TestDb.ManagementId, teacherPersonId: TestDb.Teacher2);
        Assert.Equal("Beto Vargas", Assert.Single(soloBeto.Summaries).Docente);
    }

    // ==================== (f) Resumen por docente ====================

    [Fact]
    public async Task Resumen_cuenta_reenvios_orden_por_cambios_e_incluye_docente_sin_cambios()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.AddIntern(TestDb.Teacher2, "Beto Vargas");
        db.AddIntern(102, "Carla Díaz");
        db.Context.Practices.Add(new Practice { Id = 70, Code = "P-70", Name = "Común con kit", SubjectId = 520 });
        db.Context.Practices.Add(new Practice { Id = 71, Code = "P-71", Name = "Sin kit", SubjectId = 521 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        SembrarKit(db, 70, 20, cantidad: 1, escalable: false);
        SembrarKit(db, 70, 21, cantidad: 2, escalable: false);
        SembrarKit(db, 70, 22, cantidad: 4, escalable: false);
        SembrarArticulo(db, 20, "MAT-1", "Gradilla");
        SembrarArticulo(db, 21, "MAT-2", "Termómetro");
        SembrarArticulo(db, 22, "MAT-3", "Mechero");

        // Ana: solicitud con 3 cambios (2 aumentos + 1 quitado) y 1 reenvío; otra solicitud sin cambios.
        SembrarSolicitud(db, 180, TestDb.Teacher1, new DateTime(2026, 11, 3, 14, 0, 0),
            subjectId: 520, materia: "Química", practiceId: 70, scheduleId: 1080);
        SembrarReview(db, 2501, 180, PlanningReviewAction.Submitted, new DateTime(2026, 11, 3, 14, 0, 0));
        SembrarReview(db, 2502, 180, PlanningReviewAction.Resubmitted, new DateTime(2026, 11, 3, 18, 0, 0));
        db.AddRequirement(180, 20, 3);  // kit 1 → Aumentó
        db.AddRequirement(180, 21, 5);  // kit 2 → Aumentó
        // MAT-3 sin pedido → Quitó

        SembrarSolicitud(db, 181, TestDb.Teacher1, new DateTime(2026, 11, 4, 14, 0, 0),
            subjectId: 522, materia: "Química", practiceId: 71, scheduleId: 1081);
        SembrarReview(db, 2503, 181, PlanningReviewAction.Submitted, new DateTime(2026, 11, 4, 14, 0, 0));

        // Beto: solicitud con 3 cambios (pide más de dos artículos del kit).
        SembrarSolicitud(db, 182, TestDb.Teacher2, new DateTime(2026, 11, 5, 14, 0, 0),
            subjectId: 523, materia: "Química", practiceId: 70, scheduleId: 1082);
        SembrarReview(db, 2504, 182, PlanningReviewAction.Submitted, new DateTime(2026, 11, 5, 14, 0, 0));
        db.AddRequirement(182, 20, 2);  // kit 1 → Aumentó; 21 y 22 sin pedido → Quitó

        // Carla: solicitud sin kit y sin pedidos → sin cambios de material.
        SembrarSolicitud(db, 183, 102, new DateTime(2026, 11, 6, 14, 0, 0),
            subjectId: 524, materia: "Química", practiceId: 71, scheduleId: 1083);
        SembrarReview(db, 2505, 183, PlanningReviewAction.Submitted, new DateTime(2026, 11, 6, 14, 0, 0));

        var reporte = await Servicio(db).GetMaterialChangesAsync(TestDb.ManagementId);

        Assert.Equal(6, reporte.Rows.Count);   // 3 de Ana + 3 de Beto
        Assert.Equal(3, reporte.Summaries.Count);

        var ana = reporte.Summaries[0];
        Assert.Equal("Ana Torres", ana.Docente);
        Assert.Equal(2, ana.Solicitudes);
        Assert.Equal(1, ana.ConCambios);
        Assert.Equal(3, ana.ArticulosCambiados);
        Assert.Equal(2, ana.Aumentos);
        Assert.Equal(1, ana.Quitados);
        Assert.Equal(1, ana.Reenvios);

        var beto = reporte.Summaries[1];
        Assert.Equal("Beto Vargas", beto.Docente);
        Assert.Equal(1, beto.Solicitudes);
        Assert.Equal(1, beto.ConCambios);
        Assert.Equal(3, beto.ArticulosCambiados);
        Assert.Equal(1, beto.Aumentos);
        Assert.Equal(2, beto.Quitados);

        // Carla sin cambios va al final pero aparece.
        var carla = reporte.Summaries[2];
        Assert.Equal("Carla Díaz", carla.Docente);
        Assert.Equal(1, carla.Solicitudes);
        Assert.Equal(0, carla.ConCambios);
    }

    // ==================== (f2) Reglas del 24/11: cantidad vs. artículos, 3+ artículos y 7 días ====================

    [Fact]
    public async Task Llamada_de_atencion_desde_tres_articulos_y_ultimo_momento_con_menos_de_siete_dias()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.Context.Practices.Add(new Practice { Id = 80, Code = "P-80", Name = "Majadito", SubjectId = 530 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        for (var id = 30; id <= 37; id++) SembrarArticulo(db, id, $"ART-{id}", $"Artículo {id}");
        SembrarKit(db, 80, 30, cantidad: 2, escalable: false);
        SembrarKit(db, 80, 31, cantidad: 1, escalable: false);
        SembrarKit(db, 80, 32, cantidad: 1, escalable: false);
        SembrarKit(db, 80, 33, cantidad: 3, escalable: false);

        // 190: con tiempo (02/11 → 20/11, 18 días) pero cambia 3 artículos (agrega 34 y 35, quita 31) → llamada.
        SembrarSolicitud(db, 190, TestDb.Teacher1, new DateTime(2026, 11, 2, 14, 0, 0),
            subjectId: 530, materia: "Cocina boliviana", practiceId: 80, scheduleId: 1090,
            fechaPractica: new DateTime(2026, 11, 20));
        SembrarReview(db, 2601, 190, PlanningReviewAction.Submitted, new DateTime(2026, 11, 2, 14, 0, 0));
        db.AddRequirement(190, 30, 4);   // Aumentó (cantidad)
        db.AddRequirement(190, 32, 1);   // igual
        db.AddRequirement(190, 33, 3);   // igual
        db.AddRequirement(190, 34, 1);   // Agregó
        db.AddRequirement(190, 35, 2);   // Agregó

        // 191: solo cantidades, a último momento (10/11 → 13/11, 3 días).
        SembrarSolicitud(db, 191, TestDb.Teacher1, new DateTime(2026, 11, 10, 14, 0, 0),
            subjectId: 531, materia: "Cocina boliviana", practiceId: 80, scheduleId: 1091,
            fechaPractica: new DateTime(2026, 11, 13));
        SembrarReview(db, 2602, 191, PlanningReviewAction.Submitted, new DateTime(2026, 11, 10, 14, 0, 0));
        db.AddRequirement(191, 30, 2);   // igual
        db.AddRequirement(191, 31, 2);   // Aumentó
        db.AddRequirement(191, 32, 1);   // igual
        db.AddRequirement(191, 33, 1);   // Disminuyó

        // 192: con tiempo (01/11 → 30/11) y solo 2 artículos agregados: no llega a llamada de atención.
        SembrarSolicitud(db, 192, TestDb.Teacher1, new DateTime(2026, 11, 1, 14, 0, 0),
            subjectId: 532, materia: "Cocina boliviana", practiceId: 80, scheduleId: 1092,
            fechaPractica: new DateTime(2026, 11, 30));
        SembrarReview(db, 2603, 192, PlanningReviewAction.Submitted, new DateTime(2026, 11, 1, 14, 0, 0));
        db.AddRequirement(192, 30, 2);
        db.AddRequirement(192, 31, 1);
        db.AddRequirement(192, 32, 1);
        db.AddRequirement(192, 33, 3);
        db.AddRequirement(192, 36, 1);   // Agregó
        db.AddRequirement(192, 37, 1);   // Agregó

        var reporte = await Servicio(db).GetMaterialChangesAsync(TestDb.ManagementId);

        var deLa190 = reporte.Rows.Where(f => f.Practica == "Práctica 190").ToList();
        Assert.Equal(4, deLa190.Count);
        Assert.All(deLa190, f => Assert.True(f.LlamadaAtencion));
        Assert.All(deLa190, f => Assert.False(f.UltimoMomento));
        Assert.All(deLa190, f => Assert.Equal(18, f.DiasAnticipacion));
        Assert.Equal(MaterialChangeRules.QuantityKind, deLa190.Single(f => f.Articulo.StartsWith("ART-30")).Tipo);
        Assert.Equal(MaterialChangeRules.ItemKind, deLa190.Single(f => f.Articulo.StartsWith("ART-31")).Tipo);

        var deLa191 = reporte.Rows.Where(f => f.Practica == "Práctica 191").ToList();
        Assert.Equal(2, deLa191.Count);
        Assert.All(deLa191, f => Assert.Equal(MaterialChangeRules.QuantityKind, f.Tipo));
        Assert.All(deLa191, f => Assert.False(f.LlamadaAtencion));
        Assert.All(deLa191, f => Assert.True(f.UltimoMomento));
        Assert.All(deLa191, f => Assert.Equal(3, f.DiasAnticipacion));

        var deLa192 = reporte.Rows.Where(f => f.Practica == "Práctica 192").ToList();
        Assert.Equal(2, deLa192.Count);
        Assert.All(deLa192, f => Assert.False(f.LlamadaAtencion));   // 2 artículos < 3
        Assert.All(deLa192, f => Assert.False(f.UltimoMomento));

        var ana = Assert.Single(reporte.Summaries);
        Assert.Equal(3, ana.Solicitudes);
        Assert.Equal(3, ana.ConCambios);
        Assert.Equal(1, ana.SoloCantidades);     // 191
        Assert.Equal(2, ana.CambioArticulos);    // 190 y 192
        Assert.Equal(1, ana.LlamadasAtencion);   // 190
        Assert.Equal(1, ana.UltimoMomento);      // 191
        Assert.Equal(8, ana.ArticulosCambiados);
    }

    // ==================== (g) Excel ====================

    [Fact]
    public async Task Excel_tiene_dos_hojas_con_encabezados_y_filas()
    {
        using var db = SembrarCasoPrincipal();

        var bytes = await Servicio(db).GenerateMaterialChangesExcel(TestDb.ManagementId);

        using var package = new ExcelPackage(new MemoryStream(bytes));
        Assert.Equal(2, package.Workbook.Worksheets.Count);
        var resumen = package.Workbook.Worksheets["Resumen por docente"];
        var detalle = package.Workbook.Worksheets["Detalle"];

        Assert.Contains("CAMBIOS DE MATERIAL DEL DOCENTE · 2026-2", resumen.Cells[1, 1].Value?.ToString());
        Assert.Contains("2026-2", detalle.Cells[1, 1].Value?.ToString());

        // Fila 2: leyenda con las reglas (3+ artículos = llamada de atención; menos de 7 días = último momento).
        Assert.Contains("Llamada de atención: 3 o más artículos", resumen.Cells[2, 1].Text);
        Assert.Contains("menos de 7 días", detalle.Cells[2, 1].Text);

        Assert.Equal("Docente", resumen.Cells[3, 1].Text);
        Assert.Equal("Solicitudes", resumen.Cells[3, 2].Text);
        Assert.Equal("Pedidos de cambio de L-3", resumen.Cells[3, 3].Text);
        Assert.Equal("Con cambios", resumen.Cells[3, 4].Text);
        Assert.Equal("Solo cantidades", resumen.Cells[3, 5].Text);
        Assert.Equal("Cambió artículos", resumen.Cells[3, 6].Text);
        Assert.Equal("Llamadas de atención (3+ artículos)", resumen.Cells[3, 7].Text);
        Assert.Equal("A último momento (menos de 7 días)", resumen.Cells[3, 8].Text);
        Assert.Equal("Artículos cambiados", resumen.Cells[3, 9].Text);
        Assert.Equal("Aumentos", resumen.Cells[3, 10].Text);
        Assert.Equal("Disminuciones", resumen.Cells[3, 11].Text);
        Assert.Equal("Agregados", resumen.Cells[3, 12].Text);
        Assert.Equal("Quitados", resumen.Cells[3, 13].Text);
        Assert.Equal("Reenvíos", resumen.Cells[3, 14].Text);
        Assert.Equal("Ana Torres", resumen.Cells[4, 1].Text);
        Assert.Equal(1, Convert.ToInt32(resumen.Cells[4, 2].Value));
        Assert.Equal(0, Convert.ToInt32(resumen.Cells[4, 3].Value));   // sin pedidos de cambio de L-3
        Assert.Equal(4, Convert.ToInt32(resumen.Cells[4, 9].Value));

        Assert.Equal("Origen", detalle.Cells[3, 1].Text);
        Assert.Equal("Fecha de solicitud", detalle.Cells[3, 2].Text);
        Assert.Equal("Fecha de práctica", detalle.Cells[3, 3].Text);
        Assert.Equal("Días de anticipación", detalle.Cells[3, 4].Text);
        Assert.Equal("Docente", detalle.Cells[3, 5].Text);
        Assert.Equal("Kit", detalle.Cells[3, 11].Text);
        Assert.Equal("Pedido", detalle.Cells[3, 12].Text);
        Assert.Equal("Diferencia", detalle.Cells[3, 13].Text);
        Assert.Equal("Cambio", detalle.Cells[3, 14].Text);
        Assert.Equal("Tipo", detalle.Cells[3, 15].Text);
        Assert.Equal("Alerta", detalle.Cells[3, 16].Text);
        Assert.Equal("Estado", detalle.Cells[3, 17].Text);
        Assert.Equal("Estado del cambio", detalle.Cells[3, 18].Text);
        Assert.Equal("Observación del docente", detalle.Cells[3, 19].Text);
        Assert.Equal("Solicitud", detalle.Cells[4, 1].Text);            // fila de solicitud: origen «Solicitud»
        Assert.Equal("—", detalle.Cells[4, 18].Text);                   // y sin estado de cambio
        Assert.Equal("COD-1 · Reactivo X", detalle.Cells[4, 9].Text);
        Assert.Equal("Aumentó", detalle.Cells[4, 14].Text);
        Assert.Equal("Cantidad", detalle.Cells[4, 15].Text);
        Assert.Equal(2, Convert.ToInt32(detalle.Cells[4, 4].Value));   // 03/11 → 05/11
        Assert.Equal("Último momento", detalle.Cells[4, 16].Text);      // 2 días < 7; solo 2 artículos: no es llamada

        // Columnas autoajustadas con tope: ninguna pasa de 60.
        for (var c = 1; c <= 19; c++)
            Assert.True(detalle.Column(c).Width <= 60);
    }

    [Fact]
    public async Task Excel_sin_filas_abre_con_nota_en_las_dos_hojas()
    {
        using var db = new TestDb();   // gestión sin solicitudes de docentes

        var bytes = await Servicio(db).GenerateMaterialChangesExcel(TestDb.ManagementId);

        using var package = new ExcelPackage(new MemoryStream(bytes));
        Assert.Equal(2, package.Workbook.Worksheets.Count);
        var resumen = package.Workbook.Worksheets["Resumen por docente"];
        var detalle = package.Workbook.Worksheets["Detalle"];
        Assert.Equal("Docente", resumen.Cells[3, 1].Text);
        Assert.Contains("Sin solicitudes de docentes en el rango", resumen.Cells[4, 1].Text);
        Assert.Equal("Origen", detalle.Cells[3, 1].Text);
        Assert.Contains("Sin solicitudes de docentes en el rango", detalle.Cells[4, 1].Text);
    }

    // ==================== (i) Segunda fuente: pedidos de cambio de L-3 de prácticas aprobadas ====================

    /// <summary>Práctica aprobada por importación (sin review «Enviada») con un pedido de cambio (review 8).</summary>
    private static void SembrarPedidoCambio(TestDb db, int planningId, int docenteId, DateTime utcPedido,
        int subjectId, string materia, int practiceId, int scheduleId, DateTime fechaPractica, int reviewId,
        string? motivo = null, params (int ArticuloId, decimal? Previo, decimal? Nueva)[] lineas)
    {
        SembrarSolicitud(db, planningId, docenteId, utcPedido,
            subjectId: subjectId, materia: materia, practiceId: practiceId, scheduleId: scheduleId,
            status: PlanningStatus.Approved, fechaPractica: fechaPractica);
        var review = new PlanningReview
        {
            Id = reviewId, PlanningId = planningId, Action = PlanningReviewAction.MaterialChangeRequested,
            FromStatus = PlanningStatus.Approved, ToStatus = PlanningStatus.Approved,
            Comment = motivo, UserId = 1, CreatedDate = utcPedido
        };
        foreach (var linea in lineas)
            review.Lines.Add(new PlanningReviewLine { ArticleId = linea.ArticuloId, PreviousQuantity = linea.Previo, NewQuantity = linea.Nueva });
        db.Context.PlanningReviews.Add(review);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Cambio_de_L3_agrega_filas_con_origen_y_kit_previo_contra_pedido_nuevo()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.Context.Practices.Add(new Practice { Id = 90, Code = "P-90", Name = "Volumetría", SubjectId = 540 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        SembrarArticulo(db, 40, "CAM-1", "Bureta");
        SembrarArticulo(db, 41, "CAM-2", "Soporte universal");
        SembrarArticulo(db, 42, "CAM-3", "Pipeta Pasteur");

        // Práctica aprobada sin «Enviada» (importada): su pedido de cambio del 10/11 (local) es la única fuente.
        SembrarPedidoCambio(db, 300, TestDb.Teacher1, new DateTime(2026, 11, 10, 15, 0, 0),
            subjectId: 540, materia: "Química Analítica", practiceId: 90, scheduleId: 1100,
            fechaPractica: new DateTime(2026, 11, 20), reviewId: 2701,
            motivo: "Cambio por número de estudiantes",
            (40, 5m, 8m),      // Aumentó
            (41, 2m, 1m),      // Disminuyó
            (42, null, 3m));   // Agregó

        var reporte = await Servicio(db).GetMaterialChangesAsync(TestDb.ManagementId);

        Assert.Equal(3, reporte.Rows.Count);
        Assert.All(reporte.Rows, f => Assert.Equal("Cambio de L-3", f.Origen));
        Assert.All(reporte.Rows, f => Assert.Equal("Pendiente", f.EstadoCambio));   // sin review posterior
        var bureta = reporte.Rows.Single(f => f.Articulo.Contains("CAM-1"));
        Assert.Equal(5m, bureta.Kit);                     // Kit = Previous (lo vigente)
        Assert.Equal(8m, bureta.Pedido);                  // Pedido = New (lo que pidió ahora)
        Assert.Equal(3m, bureta.Diferencia);
        Assert.Equal("Aumentó", bureta.Cambio);
        Assert.Equal(MaterialChangeRules.QuantityKind, bureta.Tipo);
        var soporte = reporte.Rows.Single(f => f.Articulo.Contains("CAM-2"));
        Assert.Equal("Disminuyó", soporte.Cambio);
        Assert.Equal(2m, soporte.Kit);
        Assert.Equal(1m, soporte.Pedido);
        var pipeta = reporte.Rows.Single(f => f.Articulo.Contains("CAM-3"));
        Assert.Equal("Agregó", pipeta.Cambio);
        Assert.Equal(0m, pipeta.Kit);
        Assert.Equal(3m, pipeta.Pedido);
        Assert.Equal("Cambio por número de estudiantes", pipeta.Observacion);   // el motivo del pedido

        // Fecha de la fila = fecha local del pedido; días de anticipación = práctica − pedido.
        Assert.Equal(BoliviaTime.ToLocal(new DateTime(2026, 11, 10, 15, 0, 0)).Date, bureta.FechaSolicitud);
        Assert.Equal(10, bureta.DiasAnticipacion);
        Assert.False(bureta.UltimoMomento);
        Assert.Equal(PlanningStatus.Approved, bureta.Estado);

        // Resumen: la docente solo tiene pedidos de cambio → 0 solicitudes, 1 cambio de L-3 que sí suma en cambios.
        var ana = Assert.Single(reporte.Summaries);
        Assert.Equal("Ana Torres", ana.Docente);
        Assert.Equal(0, ana.Solicitudes);
        Assert.Equal(1, ana.CambiosL3);
        Assert.Equal(1, ana.ConCambios);
        Assert.Equal(1, ana.CambioArticulos);      // la pipeta agregada
        Assert.Equal(0, ana.SoloCantidades);
        Assert.Equal(3, ana.ArticulosCambiados);
        Assert.Equal(1, ana.Aumentos);
        Assert.Equal(1, ana.Disminuciones);
        Assert.Equal(1, ana.Agregados);
        Assert.Equal(0, ana.Quitados);
        Assert.Equal(0, ana.Reenvios);
    }

    [Fact]
    public async Task Estado_del_cambio_por_el_siguiente_review_9_10_u_11()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.Context.Practices.Add(new Practice { Id = 91, Code = "P-91", Name = "Cromatografía", SubjectId = 541 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        SembrarArticulo(db, 43, "CAM-4", "Cromo");

        var pedido = new DateTime(2026, 11, 12, 15, 0, 0);
        var cierre = new DateTime(2026, 11, 13, 15, 0, 0);
        SembrarPedidoCambio(db, 301, TestDb.Teacher1, pedido, 541, "Química", 91, 1110, new DateTime(2026, 11, 25), 2710, lineas: [(43, 1m, 2m)]);
        SembrarPedidoCambio(db, 302, TestDb.Teacher1, pedido, 542, "Química", 91, 1111, new DateTime(2026, 11, 25), 2711, lineas: [(43, 1m, 2m)]);
        SembrarPedidoCambio(db, 303, TestDb.Teacher1, pedido, 543, "Química", 91, 1112, new DateTime(2026, 11, 25), 2712, lineas: [(43, 1m, 2m)]);
        SembrarPedidoCambio(db, 304, TestDb.Teacher1, pedido, 544, "Química", 91, 1113, new DateTime(2026, 11, 25), 2713, lineas: [(43, 1m, 2m)]);
        // Cierres posteriores: 301 aprobado, 302 rechazado, 303 retirado; 304 queda pendiente.
        SembrarReview(db, 2801, 301, PlanningReviewAction.MaterialChangeApproved, cierre);
        SembrarReview(db, 2802, 302, PlanningReviewAction.MaterialChangeRejected, cierre);
        SembrarReview(db, 2803, 303, PlanningReviewAction.MaterialChangeWithdrawn, cierre);

        var reporte = await Servicio(db).GetMaterialChangesAsync(TestDb.ManagementId);

        Assert.Equal("Aprobado", reporte.Rows.Single(f => f.Practica == "Práctica 301").EstadoCambio);
        Assert.Equal("Rechazado", reporte.Rows.Single(f => f.Practica == "Práctica 302").EstadoCambio);
        Assert.Equal("Retirado", reporte.Rows.Single(f => f.Practica == "Práctica 303").EstadoCambio);
        Assert.Equal("Pendiente", reporte.Rows.Single(f => f.Practica == "Práctica 304").EstadoCambio);
        Assert.Equal(4, Assert.Single(reporte.Summaries).CambiosL3);
    }

    [Fact]
    public async Task Cambios_de_L3_alertas_ultimo_momento_y_no_suman_en_solicitudes()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.AddIntern(TestDb.Teacher2, "Beto Vargas");
        db.Context.Practices.Add(new Practice { Id = 92, Code = "P-92", Name = "Destilación", SubjectId = 545 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        for (var id = 44; id <= 47; id++) SembrarArticulo(db, id, $"CAM-{id}", $"Artículo {id}");
        SembrarKit(db, 92, 44, cantidad: 2, escalable: false);

        // Ana: solicitud sin cambios (kit igual al pedido) + un pedido de cambio con 3 agregados → llamada de atención.
        SembrarSolicitud(db, 310, TestDb.Teacher1, new DateTime(2026, 11, 3, 14, 0, 0),
            subjectId: 545, materia: "Química", practiceId: 92, scheduleId: 1120);
        SembrarReview(db, 2810, 310, PlanningReviewAction.Submitted, new DateTime(2026, 11, 3, 14, 0, 0));
        db.AddRequirement(310, 44, 2);
        SembrarPedidoCambio(db, 311, TestDb.Teacher1, new DateTime(2026, 11, 12, 15, 0, 0),
            546, "Química", 92, 1121, new DateTime(2026, 11, 25), 2811, motivo: null,
            (45, null, 1m), (46, null, 2m), (47, null, 3m));

        // Beto: SOLO un pedido de cambio, pedido a último momento (22/11 → 25/11, 3 días).
        SembrarPedidoCambio(db, 312, TestDb.Teacher2, new DateTime(2026, 11, 22, 15, 0, 0),
            547, "Química", 92, 1122, new DateTime(2026, 11, 25), 2812, motivo: null,
            (44, 2m, 4m));

        var reporte = await Servicio(db).GetMaterialChangesAsync(TestDb.ManagementId);

        // Filas: 3 del pedido de Ana + 1 del pedido de Beto; la solicitud de Ana no aporta (sin cambios).
        Assert.Equal(4, reporte.Rows.Count);
        var deAna = reporte.Rows.Where(f => f.Docente == "Ana Torres").ToList();
        Assert.All(deAna, f =>
        {
            Assert.True(f.LlamadaAtencion);    // 3 artículos agregados
            Assert.False(f.UltimoMomento);     // 13 días de anticipación
        });
        var deBeto = reporte.Rows.Single(f => f.Docente == "Beto Vargas");
        Assert.True(deBeto.UltimoMomento);
        Assert.False(deBeto.LlamadaAtencion);

        var ana = reporte.Summaries.Single(s => s.Docente == "Ana Torres");
        Assert.Equal(1, ana.Solicitudes);      // la solicitud cuenta en Solicitudes; el pedido no
        Assert.Equal(1, ana.CambiosL3);
        Assert.Equal(1, ana.LlamadasAtencion);
        Assert.Equal(1, ana.ConCambios);       // solo el pedido: la solicitud no tenía cambios

        var beto = reporte.Summaries.Single(s => s.Docente == "Beto Vargas");
        Assert.Equal(0, beto.Solicitudes);     // solo pedidos de cambio: aparece igual
        Assert.Equal(1, beto.CambiosL3);
        Assert.Equal(1, beto.UltimoMomento);
        Assert.Equal(1, beto.ConCambios);
        Assert.Equal(1, beto.Aumentos);
    }

    [Fact]
    public async Task Excel_marca_el_origen_y_el_estado_del_cambio()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.Context.Practices.Add(new Practice { Id = 93, Code = "P-93", Name = "Filtración", SubjectId = 548 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        SembrarArticulo(db, 48, "CAM-8", "Papel filtro");

        SembrarPedidoCambio(db, 320, TestDb.Teacher1, new DateTime(2026, 11, 10, 15, 0, 0),
            548, "Química", 93, 1130, new DateTime(2026, 11, 20), 2820, lineas: [(48, 5m, 8m)]);

        var bytes = await Servicio(db).GenerateMaterialChangesExcel(TestDb.ManagementId);

        using var package = new ExcelPackage(new MemoryStream(bytes));
        var detalle = package.Workbook.Worksheets["Detalle"];
        Assert.Equal("Cambio de L-3", detalle.Cells[4, 1].Text);
        Assert.Equal("Pendiente", detalle.Cells[4, 18].Text);
        Assert.Equal(5m, Convert.ToDecimal(detalle.Cells[4, 11].Value));
        Assert.Equal(8m, Convert.ToDecimal(detalle.Cells[4, 12].Value));
        Assert.Equal("Aumentó", detalle.Cells[4, 14].Text);
        // El resumen muestra su contador propio: 0 solicitudes, 1 pedido de cambio de L-3.
        var resumen = package.Workbook.Worksheets["Resumen por docente"];
        Assert.Equal(0, Convert.ToInt32(resumen.Cells[4, 2].Value));
        Assert.Equal(1, Convert.ToInt32(resumen.Cells[4, 3].Value));
    }

    // ==================== (h) ScaleKit ====================

    [Fact]
    public void ScaleKit_suma_por_articulo_null_cuenta_uno_y_coincide_con_Scale()
    {
        // Suma por artículo cuando la receta repite líneas.
        var suma = RequirementPickerService.ScaleKit(
        [
            new RequirementPickerService.KitLine(1, 2m, false),
            new RequirementPickerService.KitLine(1, 3m, false),
            new RequirementPickerService.KitLine(2, 5m, false)
        ], students: null, factor: null);
        Assert.Equal(2, suma.Count);
        Assert.Equal(5m, suma[1].Quantity);
        Assert.Equal(5m, suma[2].Quantity);

        // Sin cantidad registrada: la línea cuenta 1.
        var conNull = RequirementPickerService.ScaleKit(
            [new RequirementPickerService.KitLine(3, null, false)], null, null);
        Assert.Equal(1m, conNull[3].Quantity);

        // El escalado coincide con Scale línea por línea (1 cada 4, con 20 estudiantes → 5).
        var escalado = RequirementPickerService.ScaleKit(
            [new RequirementPickerService.KitLine(4, 1m, true)], students: 20, factor: 4);
        var (esperado, _) = RequirementPickerService.Scale(1m, true, 20, 4);
        Assert.Equal(esperado, escalado[4].Quantity);
        Assert.NotNull(escalado[4].Note);
    }
}
