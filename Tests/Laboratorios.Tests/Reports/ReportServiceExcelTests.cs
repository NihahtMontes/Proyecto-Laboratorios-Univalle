using Laboratorios.Tests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using OfficeOpenXml;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests.Reports;

/// <summary>
/// Pruebas de los generadores de Excel (EPPlus) contra las plantillas reales de
/// wwwroot/templates: cada reporte debe producir un .xlsx que EPPlus puede reabrir,
/// sin datos de ejemplo de la plantilla y sin perder información (nota visible de
/// desbordamiento en L-12 cuando los ítems exceden las filas del formulario).
/// La base es EF InMemory (TestDb); no se toca SQL Server ni las plantillas.
/// </summary>
public class ReportServiceExcelTests
{
    static ReportServiceExcelTests() => ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    /// <summary>Ubica la raíz del proyecto web para resolver wwwroot/templates (como LabAppHost).</summary>
    private static string WebProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Proyecto Laboratorios Univalle.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("No se encontró el proyecto web.");
    }

    private static string TemplatePath(string nombre) =>
        Path.Combine(WebProjectRoot(), "wwwroot", "templates", nombre);

    /// <summary>IWebHostEnvironment con WebRootPath apuntando al wwwroot real del proyecto web.</summary>
    private sealed class WebRootEnvironment : IWebHostEnvironment
    {
        public WebRootEnvironment()
        {
            ContentRootPath = WebProjectRoot();
            WebRootPath = Path.Combine(ContentRootPath, "wwwroot");
            ContentRootFileProvider = new NullFileProvider();
            WebRootFileProvider = new NullFileProvider();
        }

        public string ApplicationName { get; set; } = "Laboratorios.Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
    }

    private static ReportService Servicio(TestDb db) =>
        new(db.Context, new WebRootEnvironment(), NullLogger<ReportService>.Instance);

    /// <summary>Reabre el archivo generado: si EPPlus no puede abrirlo, el test falla aquí.</summary>
    private static ExcelPackage Abrir(byte[] bytes) => new(new MemoryStream(bytes));

    // ==================== L-12 · Adquisición ====================

    [Fact]
    public async Task Adquisicion_con_45_items_deja_nota_de_omitidos_y_total_completo()
    {
        using var db = new TestDb();
        var servicio = Servicio(db);

        db.Context.Faculties.Add(new Faculty { Id = 5, Name = "Facultad de Pruebas" });
        db.Context.Laboratories.Add(new Laboratory { Id = 20, FacultyId = 5, Code = "LP", Name = "Laboratorio Prueba" });
        var solicitante = new Extern
        {
            Id = 60,
            Name = "María Pérez",
            Category = PersonCategory.Externo,
            ExternStatus = GeneralStatus.Activo,
            IsEntity = false,
            Address = "No requiere"
        };
        db.Context.Externs.Add(solicitante);
        db.Context.SaveChanges();

        var solicitud = new Request
        {
            Id = 50,
            Type = RequestType.Purchasing,
            ManagementId = TestDb.ManagementId,
            LaboratoryId = 20,
            Laboratory = db.Context.Laboratories.Find(20),
            RequestedByPersonId = 60,
            RequestedByPerson = solicitante,
            RequestDate = new DateTime(2026, 3, 15),
            CreatedDate = new DateTime(2026, 3, 20, 8, 0, 0, DateTimeKind.Utc),
            Description = "Compra de reactivos"
        };
        for (var i = 1; i <= 45; i++)
        {
            solicitud.CostDetails.Add(new CostDetail
            {
                RequestId = 50,
                Concept = $"Reactivo {i:00}",
                Quantity = 1,
                UnitPrice = 10m,
                UnitOfMeasure = "UNIDAD"
            });
        }
        db.Context.Requests.Add(solicitud);
        db.Context.SaveChanges();

        var bytes = await servicio.GenerateSolicitudAdquisicionExcel(solicitud);

        using var package = Abrir(bytes);
        var ws = package.Workbook.Worksheets[0];

        // La fecha del documento es la de la solicitud, no la del sistema.
        Assert.Equal(15, Convert.ToInt32(ws.Cells[10, 4].Value)); // Día (D10:E11)
        Assert.Equal(3, Convert.ToInt32(ws.Cells[10, 6].Value));  // Mes (F10:G11)
        Assert.Equal(2026, Convert.ToInt32(ws.Cells[10, 8].Value)); // Año (H10:I11)

        // Responsable histórico desde RequestedByPerson (sin usuario solicitante).
        Assert.Equal("MARÍA PÉREZ", ws.Cells["D8"].Text.Trim());

        // 16 ítems visibles (filas 19..34; 35-36 es el bloque de observaciones de la plantilla).
        Assert.Equal("Reactivo 01", ws.Cells[19, 5].Text.Trim());
        Assert.Equal("Reactivo 16", ws.Cells[34, 5].Text.Trim());

        // La nota de desbordamiento queda visible en la última fila de ítems (37): 45 - 16 = 29.
        Assert.Contains("29 ÍTEMS MÁS", ws.Cells[37, 5].Text);

        // El total del documento suma los 45 ítems, no solo los impresos.
        Assert.Equal(450m, Convert.ToDecimal(ws.Cells[38, 17].Value));
    }

    [Fact]
    public async Task Adquisicion_sin_fecha_usa_createddate_y_usuario_solicitante()
    {
        using var db = new TestDb();
        var servicio = Servicio(db);

        db.Context.Faculties.Add(new Faculty { Id = 5, Name = "Facultad de Pruebas" });
        db.Context.Laboratories.Add(new Laboratory { Id = 20, FacultyId = 5, Code = "LP", Name = "Laboratorio Prueba" });
        db.Context.Users.Add(new User { Id = 77, UserName = "alopez", FirstName = "Ana", LastName = "López" });
        db.Context.SaveChanges();

        var solicitud = new Request
        {
            Id = 51,
            Type = RequestType.Purchasing,
            ManagementId = TestDb.ManagementId,
            LaboratoryId = 20,
            Laboratory = db.Context.Laboratories.Find(20),
            RequestedById = 77,
            RequestedBy = db.Context.Users.Find(77),
            RequestDate = null,
            CreatedDate = new DateTime(2025, 11, 2, 10, 0, 0, DateTimeKind.Utc),
            Description = "Compra menor"
        };
        solicitud.CostDetails.Add(new CostDetail { RequestId = 51, Concept = "Cable HDMI", Quantity = 2, UnitPrice = 25m });
        db.Context.Requests.Add(solicitud);
        db.Context.SaveChanges();

        var bytes = await servicio.GenerateSolicitudAdquisicionExcel(solicitud);

        using var package = Abrir(bytes);
        var ws = package.Workbook.Worksheets[0];

        // Sin RequestDate se usa CreatedDate (02/11/2025).
        Assert.Equal(2, Convert.ToInt32(ws.Cells[10, 4].Value));
        Assert.Equal(11, Convert.ToInt32(ws.Cells[10, 6].Value));
        Assert.Equal(2025, Convert.ToInt32(ws.Cells[10, 8].Value));

        // El usuario solicitante tiene prioridad sobre la persona histórica.
        Assert.Equal("ANA LÓPEZ", ws.Cells["D8"].Text.Trim());

        // Con 1 ítem no hay nota de desbordamiento.
        Assert.DoesNotContain("ÍTEMS MÁS", ws.Cells[37, 5].Text);
        Assert.Equal(50m, Convert.ToDecimal(ws.Cells[38, 17].Value));
    }

    // ==================== L-3 · Salidas ====================

    [Fact]
    public void L3_plantilla_tiene_cinco_hojas_form_l33()
    {
        // Evidencia de los nombres reales de la plantilla (contrato para eliminar las no usadas).
        using var package = new ExcelPackage(new FileInfo(TemplatePath("L3.xlsx")));
        Assert.Equal(5, package.Workbook.Worksheets.Count);
        Assert.All(package.Workbook.Worksheets, hoja =>
            Assert.StartsWith("FORM L-3.3", hoja.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task L3_generado_deja_una_sola_hoja_y_sin_datos_de_ejemplo()
    {
        using var db = new TestDb();
        var servicio = Servicio(db);

        var salida = new Departure
        {
            Id = 30,
            ManagementId = TestDb.ManagementId,
            OriginLaboratoryId = TestDb.LabA,
            DepartureDate = new DateTime(2026, 10, 6),
            Code = "SAL-00001"
        };
        salida.Items.Add(new DepartureItem { ProductName = "BALANZA DIGITAL", Quantity = 1m, UnitOfMeasure = "PZA", ReturnedQuantity = 0m });
        db.Context.Departures.Add(salida);
        db.Context.SaveChanges();

        var bytes = await servicio.GenerateL3SalidaExcel(30);

        using var package = Abrir(bytes);
        Assert.Equal(1, package.Workbook.Worksheets.Count);
        var ws = package.Workbook.Worksheets[0];
        Assert.Equal("FORM L-3.3 (5)", ws.Name);

        var textos = ws.Cells[ws.Dimension.Address].Select(c => c.Text).ToList();
        Assert.Contains(textos, t => t.Contains("BALANZA DIGITAL"));
        // Los ítems de ejemplo de la plantilla no deben llegar al archivo generado.
        Assert.DoesNotContain(textos, t => t.Contains("PINZAS DE HIELO"));
    }

    // ==================== L-6 · Verificaciones ====================

    [Fact]
    public async Task L6_con_dos_planes_del_mismo_equipo_no_duplica_filas()
    {
        using var db = new TestDb();
        var servicio = Servicio(db);

        db.Context.Equipments.Add(new Equipment { Id = 300, Name = "Microscopio", Brand = "Olympus" });
        db.Context.EquipmentUnits.Add(new EquipmentUnit
        {
            Id = 301,
            EquipmentId = 300,
            LaboratoryId = TestDb.LabA,
            InventoryNumber = "INV-001",
            CurrentStatus = EquipmentStatus.Operational
        });
        db.Context.ManagementPlans.AddRange(
            new ManagementPlan { Id = 401, ManagementId = TestDb.ManagementId, EquipmentUnitId = 301 },
            new ManagementPlan { Id = 402, ManagementId = TestDb.ManagementId, EquipmentUnitId = 301 });
        db.Context.SaveChanges();

        var bytes = await servicio.GenerateL6VerificacionExcel(TestDb.ManagementId, TestDb.LabA, "Responsable de Prueba");

        using var package = Abrir(bytes);
        Assert.Equal(1, package.Workbook.Worksheets.Count);
        var ws = package.Workbook.Worksheets[0];

        // Encabezado en fila 12 (col. C = "# DE INV"); datos desde la 13. Un mismo equipo,
        // aunque tenga dos planes en la gestión, aparece una sola vez.
        var apariciones = Enumerable.Range(13, 30)
            .Count(r => string.Equals(ws.Cells[r, 3].Text.Trim(), "INV-001", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, apariciones);
    }

    // ==================== L-7 · Solicitud técnica ====================

    [Fact]
    public async Task L7_solicitud_tecnica_genera_xlsx_abrible()
    {
        using var db = new TestDb();
        var servicio = Servicio(db);

        var solicitud = new Request
        {
            Id = 70,
            Type = RequestType.Technical,
            ManagementId = TestDb.ManagementId,
            Description = "Equipo no enciende",
            RequestDate = new DateTime(2026, 10, 1),
            CreatedDate = new DateTime(2026, 10, 1)
        };
        db.Context.Requests.Add(solicitud);
        db.Context.SaveChanges();

        var bytes = await servicio.GenerateSolicitudMantenimientoExcel(solicitud);

        using var package = Abrir(bytes);
        var ws = package.Workbook.Worksheets[0];
        Assert.StartsWith("SOLICITUD MANTENIMIENTO", ws.Cells["A2"].Text.Trim());
        // Código de registro con el id exacto de la solicitud.
        Assert.Equal("RE-10-LAB-070", ws.Cells["D4"].Text.Trim());
    }

    // ==================== L-8 · Kardex ====================

    [Fact]
    public async Task L8_kardex_genera_xlsx_abrible_con_datos_del_equipo()
    {
        using var db = new TestDb();
        var servicio = Servicio(db);

        db.Context.Equipments.Add(new Equipment { Id = 310, Name = "Centrífuga", Brand = "Eppendorf" });
        db.Context.EquipmentUnits.Add(new EquipmentUnit
        {
            Id = 311,
            EquipmentId = 310,
            LaboratoryId = TestDb.LabA,
            InventoryNumber = "INV-777"
        });
        db.Context.SaveChanges();

        var bytes = await servicio.GenerateL8KardexExcel(311);

        using var package = Abrir(bytes);
        var ws = package.Workbook.Worksheets[0];
        Assert.Equal("CENTRÍFUGA", ws.Cells["B8"].Text.Trim());
        Assert.Equal("LABORATORIO A", ws.Cells["B9"].Text.Trim());
    }
}
