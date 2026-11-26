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
/// Informe L-9 de reprogramaciones y reposiciones: distingue el tipo de cada movimiento, muestra
/// origen/destino con los periodos y horas reales del catálogo, aplica el filtro de gestión (y el
/// rango opcional por fecha original) y produce un Excel abrible también cuando no hay filas.
/// La base es EF InMemory (TestDb); no se toca SQL Server ni se generan archivos en disco.
/// </summary>
public class L9ReportTests
{
    static L9ReportTests() => ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    private const int UsuarioRegistroId = 77;
    private static readonly DateTime FechaRegistroReprogramacion = new(2026, 10, 5, 12, 0, 0);
    private static readonly DateTime FechaRegistroReposicion = new(2026, 10, 6, 12, 0, 0);

    /// <summary>IWebHostEnvironment mínimo: el L-9 no usa plantillas, pero el constructor lo pide.</summary>
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

    private static Rescheduling Movimiento(int id, int origenId, int destinoId, DateTime fechaOrigen,
        int inicioOrigen, int cantidadOrigen, int labOrigen, DateTime fechaDestino, int inicioDestino,
        int cantidadDestino, int labDestino, string motivo, int? reposicionDiaId = null,
        DateTime? registro = null) => new()
    {
        Id = id,
        OriginPlanningId = origenId,
        DestinationPlanningId = destinoId,
        OriginDate = fechaOrigen,
        OriginStartPeriodId = inicioOrigen,
        OriginPeriodCount = cantidadOrigen,
        OriginLaboratoryId = labOrigen,
        OriginTeacherPersonId = TestDb.Teacher1,
        OriginStatus = PlanningStatus.Planned,
        DestinationDate = fechaDestino,
        DestinationStartPeriodId = inicioDestino,
        DestinationPeriodCount = cantidadDestino,
        DestinationLaboratoryId = labDestino,
        DestinationTeacherPersonId = TestDb.Teacher1,
        Reason = motivo,
        DayRecoveryId = reposicionDiaId,
        CreatedById = UsuarioRegistroId,
        CreatedDate = registro ?? fechaOrigen
    };

    /// <summary>Siembra una reprogramación (06/10 → 07/10) y una reposición del día suspendido (14/10 → 15/10).</summary>
    private static TestDb SembrarGestion1()
    {
        var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Torres");
        db.Context.Users.Add(new User
        {
            Id = UsuarioRegistroId,
            UserName = "jlopez",
            FirstName = "Julia",
            LastName = "López"
        });
        db.AddPlanning(10, TestDb.Week1Monday.AddDays(1), 1, 2);   // martes 06/10, P1–2, LAB-A
        db.AddPlanning(20, TestDb.Week1Monday.AddDays(2), 3, 2, laboratoryId: TestDb.LabB);  // miércoles 07/10, P3–4, LAB-B
        db.AddPlanning(11, TestDb.SuspendedDay, 1, 2, status: PlanningStatus.PendingRecovery, weekId: TestDb.Week2Id);
        db.AddPlanning(21, TestDb.SuspendedDay.AddDays(1), 4, 2, weekId: TestDb.Week2Id);
        db.Context.DayRecoveries.Add(new DayRecovery
        {
            Id = 1,
            ManagementId = TestDb.ManagementId,
            Date = TestDb.SuspendedDay,
            Reason = TestDb.SuspendedReason,
            ReasonCategory = "Feriado"
        });
        db.Context.Reschedulings.AddRange(
            Movimiento(1, 10, 20, TestDb.Week1Monday.AddDays(1), 1, 2, TestDb.LabA,
                TestDb.Week1Monday.AddDays(2), 3, 2, TestDb.LabB,
                "Cambio de horario del docente", registro: FechaRegistroReprogramacion),
            Movimiento(2, 11, 21, TestDb.SuspendedDay, 1, 2, TestDb.LabA,
                TestDb.SuspendedDay.AddDays(1), 4, 2, TestDb.LabA,
                "Reubicación de la práctica suspendida", reposicionDiaId: 1, registro: FechaRegistroReposicion));
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        return db;
    }

    /// <summary>Planificación en otra gestión (TestDb.AddPlanning fija la gestión por defecto).</summary>
    private static void AddPlanningEn(TestDb db, int id, int managementId, DateTime date, int startOrder, int count)
    {
        var planning = new Planning
        {
            Id = id,
            ManagementId = managementId,
            LaboratoryId = TestDb.LabA,
            TeacherPersonId = TestDb.Teacher1,
            PracticeName = $"Práctica {id}",
            GroupName = "B",
            Status = PlanningStatus.Planned,
            RequestDate = date,
            Subject = new Subject { Id = id * 10, Name = "Física" }
        };
        PlanningSlot.Apply(planning, date, startOrder, count, db.Periods());
        db.Context.Plannings.Add(planning);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static string HorasEsperadas(TestDb db, int ordenInicial, int cantidad)
    {
        var porOrden = db.Periods().ToDictionary(p => p.Order);
        var inicio = porOrden[ordenInicial];
        var fin = porOrden[ordenInicial + cantidad - 1];
        return $"{inicio.StartTime:hh\\:mm}–{fin.EndTime:hh\\:mm}";
    }

    /// <summary>EPPlus puede devolver la fecha como DateTime o como serial OADate (Double).</summary>
    private static DateTime FechaCelda(ExcelRange celda) =>
        celda.Value is DateTime fecha ? fecha.Date : DateTime.FromOADate(Convert.ToDouble(celda.Value)).Date;

    // ==================== Consulta de filas ====================

    [Fact]
    public async Task L9_distingue_reprogramacion_y_reposicion_con_origen_destino_y_motivo()
    {
        using var db = SembrarGestion1();

        var filas = await Servicio(db).GetL9ReportRowsAsync(TestDb.ManagementId);

        Assert.Equal(2, filas.Count);

        // Orden por fecha de registro y numeración consecutiva desde 1.
        Assert.Equal("Reprogramación", filas[0].Tipo);
        Assert.Equal("Reposición", filas[1].Tipo);
        Assert.Equal(1, filas[0].Numero);
        Assert.Equal(2, filas[1].Numero);

        var reprogramacion = filas[0];
        Assert.Equal("PRA-00020", reprogramacion.CodigoPractica);      // código visible de la planificación destino
        Assert.Equal("Química", reprogramacion.Materia);
        Assert.Equal("Práctica 20", reprogramacion.Practica);
        Assert.Equal("A", reprogramacion.Grupo);
        Assert.Equal("Ana Torres", reprogramacion.Docente);
        Assert.Equal(TestDb.Week1Monday.AddDays(1), reprogramacion.FechaOrigen);
        Assert.Equal($"Periodos 1–2 · {HorasEsperadas(db, 1, 2)}", reprogramacion.PeriodosOrigen);
        Assert.Equal("LAB-A · Laboratorio A", reprogramacion.LaboratorioOrigen);
        Assert.Equal(TestDb.Week1Monday.AddDays(2), reprogramacion.FechaDestino);
        Assert.Equal($"Periodos 3–4 · {HorasEsperadas(db, 3, 2)}", reprogramacion.PeriodosDestino);
        Assert.Equal("LAB-B · Laboratorio B", reprogramacion.LaboratorioDestino);
        Assert.Equal("Cambio de horario del docente", reprogramacion.Motivo);
        Assert.Equal("Julia López", reprogramacion.RegistradoPor);
        Assert.Equal(BoliviaTime.ToLocal(FechaRegistroReprogramacion), reprogramacion.FechaRegistro);

        var reposicion = filas[1];
        Assert.Equal("PRA-00021", reposicion.CodigoPractica);
        Assert.Equal(TestDb.SuspendedDay, reposicion.FechaOrigen);
        Assert.Equal(TestDb.SuspendedDay.AddDays(1), reposicion.FechaDestino);
        // El motivo de una reposición es el de la suspensión del día (categoría + detalle).
        Assert.Equal("Feriado: Feriado departamental", reposicion.Motivo);
    }

    [Fact]
    public async Task L9_el_filtro_de_gestion_excluye_los_movimientos_de_otras()
    {
        using var db = SembrarGestion1();
        db.AddManagement(2, "2025-1", 2025, 1);
        AddPlanningEn(db, 30, 2, TestDb.Week1Monday.AddDays(3), 1, 1);
        db.Context.Reschedulings.Add(Movimiento(3, 30, 30, TestDb.Week1Monday.AddDays(3), 1, 1, TestDb.LabA,
            TestDb.Week1Monday.AddDays(4), 5, 1, TestDb.LabA, "Ajuste de la gestión anterior",
            registro: new DateTime(2025, 6, 1, 12, 0, 0)));
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var gestionActual = await Servicio(db).GetL9ReportRowsAsync(TestDb.ManagementId);
        Assert.Equal(2, gestionActual.Count);
        Assert.All(gestionActual, f => Assert.Equal("Química", f.Materia));

        var gestionAnterior = await Servicio(db).GetL9ReportRowsAsync(2);
        var fila = Assert.Single(gestionAnterior);
        Assert.Equal("Física", fila.Materia);
        Assert.Equal("PRA-00030", fila.CodigoPractica);
    }

    [Fact]
    public async Task L9_el_rango_opcional_filtra_por_fecha_original()
    {
        using var db = SembrarGestion1();

        // 07/10 no tiene movimientos de origen: el rango exacto de ese día queda vacío, sin errores.
        var vacio = await Servicio(db).GetL9ReportRowsAsync(TestDb.ManagementId,
            originFrom: new DateTime(2026, 10, 7), originTo: new DateTime(2026, 10, 7));
        Assert.Empty(vacio);

        // Desde el miércoles 07/10 queda solo la reposición (el 06/10 es la reprogramación).
        var desdeElMiercoles = await Servicio(db).GetL9ReportRowsAsync(TestDb.ManagementId,
            originFrom: new DateTime(2026, 10, 7));
        var fecha = Assert.Single(desdeElMiercoles);
        Assert.Equal("Reposición", fecha.Tipo);

        var hastaElMiercoles = await Servicio(db).GetL9ReportRowsAsync(TestDb.ManagementId,
            originTo: new DateTime(2026, 10, 6));
        var reprogramacion = Assert.Single(hastaElMiercoles);
        Assert.Equal("Reprogramación", reprogramacion.Tipo);
    }

    [Fact]
    public async Task L9_sin_periodos_registrados_lo_dice_en_lugar_de_inventar_horas()
    {
        using var db = SembrarGestion1();

        // Movimiento legado sin periodo de origen (horas solo, sin bloque de periodos).
        db.Context.Reschedulings.Add(new Rescheduling
        {
            Id = 4,
            OriginPlanningId = 10,
            DestinationPlanningId = 20,
            OriginDate = TestDb.Week1Monday.AddDays(4),
            OriginStartPeriodId = null,
            OriginPeriodCount = null,
            OriginLaboratoryId = TestDb.LabA,
            OriginTeacherPersonId = TestDb.Teacher1,
            OriginStatus = PlanningStatus.Planned,
            DestinationDate = TestDb.Week1Monday.AddDays(4),
            DestinationStartPeriodId = 1,
            DestinationPeriodCount = 1,
            DestinationLaboratoryId = TestDb.LabA,
            DestinationTeacherPersonId = TestDb.Teacher1,
            Reason = "Origen sin periodos",
            CreatedById = UsuarioRegistroId,
            CreatedDate = new DateTime(2026, 10, 7, 12, 0, 0)
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var filas = await Servicio(db).GetL9ReportRowsAsync(TestDb.ManagementId);

        Assert.Equal(3, filas.Count);

        // El legado es el último por fecha de registro (07/10).
        var legada = filas[2];
        Assert.Equal("Sin periodos", legada.PeriodosOrigen);
        Assert.StartsWith("Periodo 1 ·", legada.PeriodosDestino);
    }

    // ==================== Excel generado ====================

    [Fact]
    public async Task L9_excel_tiene_encabezado_institucional_y_las_filas_consultadas()
    {
        using var db = SembrarGestion1();

        var bytes = await Servicio(db).GenerateL9ReprogramacionesExcel(TestDb.ManagementId);

        using var package = new ExcelPackage(new MemoryStream(bytes));
        var ws = package.Workbook.Worksheets.Single();
        Assert.Equal("L-9", ws.Name);

        Assert.Contains("REPROGRAMACIONES Y REPOSICIONES", ws.Cells[1, 1].Value?.ToString());
        Assert.Contains("Gestión: II/2026", ws.Cells[2, 1].Value?.ToString());
        Assert.Contains("Fecha de emisión:", ws.Cells[4, 1].Value?.ToString());

        Assert.Equal("N.º", ws.Cells[6, 1].Text);
        Assert.Equal("Tipo", ws.Cells[6, 2].Text);
        Assert.Equal("Motivo", ws.Cells[6, 14].Text);

        // Filas ordenadas por fecha de registro: primero la reprogramación, luego la reposición.
        Assert.Equal(1, Convert.ToInt32(ws.Cells[7, 1].Value));
        Assert.Equal("Reprogramación", ws.Cells[7, 2].Text);
        Assert.Equal("Cambio de horario del docente", ws.Cells[7, 14].Text);
        Assert.Equal(2, Convert.ToInt32(ws.Cells[8, 1].Value));
        Assert.Equal("Reposición", ws.Cells[8, 2].Text);
        Assert.Equal("Feriado: Feriado departamental", ws.Cells[8, 14].Text);

        // Fechas reales (no texto) con su formato día/mes.
        Assert.Equal(TestDb.Week1Monday.AddDays(1), FechaCelda(ws.Cells[7, 8]));
        Assert.Equal(TestDb.SuspendedDay.AddDays(1), FechaCelda(ws.Cells[8, 11]));

        // El rango con autofiltros cubre encabezado + datos.
        Assert.NotNull(ws.AutoFilter?.Address);
    }

    [Fact]
    public async Task L9_excel_sin_filas_genera_archivo_abrible_con_nota()
    {
        using var db = new TestDb();   // gestión sin reprogramaciones ni reposiciones

        var bytes = await Servicio(db).GenerateL9ReprogramacionesExcel(TestDb.ManagementId);

        using var package = new ExcelPackage(new MemoryStream(bytes));
        var ws = package.Workbook.Worksheets.Single();
        Assert.Equal("N.º", ws.Cells[6, 1].Text);
        Assert.Contains("Sin reprogramaciones ni reposiciones", ws.Cells[7, 1].Text);
    }
}
