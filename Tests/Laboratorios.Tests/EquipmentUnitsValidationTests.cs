using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.EquipmentUnits;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>
/// Validaciones del alta/edición de unidades físicas (EquipmentUnits).
/// QA reportó error 500 con entradas inválidas: estas pruebas fijan que el InputModel
/// rechace por anotación (longitudes/precio) y que el PageModel rechace por FK inexistente,
/// laboratorio ajeno a la facultad, fechas incoherentes y estados no asignables a mano.
/// </summary>
public class EquipmentUnitsValidationTests
{
    private static List<ValidationResult> Validate(object input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
        return results;
    }

    /// <summary>
    /// Inicializa PageContext del PageModel (fuera del pipeline MVC es null y las rutas de
    /// error llaman a LoadLists, que escribe en ViewData).
    /// </summary>
    private static TPage Prepare<TPage>(TPage page) where TPage : PageModel
    {
        page.PageContext = new PageContext
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
        return page;
    }

    private static string NewString(int length) => new('A', length);

    // ═══════════════ Anotaciones del InputModel (Create) ═══════════════

    [Fact]
    public void Create_InventarioDeDosCaracteres_SeRechaza()
    {
        var errors = Validate(new CreateModel.InputModel { InventoryNumber = "AB" });

        Assert.Contains(errors, e => e.ErrorMessage == "El inventario debe tener al menos 3 caracteres");
    }

    [Fact]
    public void Create_InventarioDeCincuentaYUnCaracteres_SeRechaza()
    {
        var errors = Validate(new CreateModel.InputModel { InventoryNumber = NewString(51) });

        Assert.Contains(errors, e => e.ErrorMessage == "El inventario no puede superar 50 caracteres");
    }

    [Fact]
    public void Create_PrecioNegativo_SeRechaza()
    {
        var errors = Validate(new CreateModel.InputModel { InventoryNumber = "UV-001", AcquisitionValue = -1m });

        Assert.Contains(errors, e => e.ErrorMessage == "El precio de adquisición no puede ser negativo");
    }

    [Fact]
    public void Create_SerieDeCientoUnCaracteres_SeRechaza()
    {
        var errors = Validate(new CreateModel.InputModel { InventoryNumber = "UV-001", SerialNumber = NewString(101) });

        Assert.Contains(errors, e => e.ErrorMessage == "El número de serie no puede superar 100 caracteres");
    }

    [Fact]
    public void Create_NotasDeDosMilUnCaracteres_SeRechazan()
    {
        var errors = Validate(new CreateModel.InputModel { InventoryNumber = "UV-001", Notes = NewString(2001) });

        Assert.Contains(errors, e => e.ErrorMessage == "Las observaciones no pueden superar 2000 caracteres");
    }

    // ═══════════════ Anotaciones del InputModel (Edit) ═══════════════

    [Fact]
    public void Edit_InventarioDeDosCaracteres_SeRechaza()
    {
        var errors = Validate(new EditModel.InputModel { InventoryNumber = "AB" });

        Assert.Contains(errors, e => e.ErrorMessage == "El inventario debe tener al menos 3 caracteres");
    }

    [Fact]
    public void Edit_InventarioDeCincuentaYUnCaracteres_SeRechaza()
    {
        var errors = Validate(new EditModel.InputModel { InventoryNumber = NewString(51) });

        Assert.Contains(errors, e => e.ErrorMessage == "El inventario no puede superar 50 caracteres");
    }

    [Fact]
    public void Edit_PrecioNegativo_SeRechaza()
    {
        var errors = Validate(new EditModel.InputModel { InventoryNumber = "UV-001", AcquisitionValue = -1m });

        Assert.Contains(errors, e => e.ErrorMessage == "El precio de adquisición no puede ser negativo");
    }

    // ═══════════════ Rechazos del PageModel sin HttpContext ═══════════════

    [Fact]
    public async Task Edit_OnPost_ModeloDeEquipoInexistente_DevuelvePageConError()
    {
        using var db = new TestDb();
        var page = Prepare(new EditModel(db.Context));
        page.EquipmentUnit = new EditModel.InputModel
        {
            Id = 1,
            EquipmentId = 999, // FK inexistente
            InventoryNumber = "UV-001",
            CurrentStatus = EquipmentStatus.Operational,
            PhysicalCondition = PhysicalCondition.Good
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        var entry = page.ModelState["EquipmentUnit.EquipmentId"];
        Assert.NotNull(entry);
        Assert.Contains(entry!.Errors, e => e.ErrorMessage == "El modelo de equipo seleccionado no existe.");
    }

    [Fact]
    public async Task Edit_OnPost_EstadoOnLoanManual_DevuelvePageConError()
    {
        using var db = new TestDb();
        db.Context.Equipments.Add(new Equipment { Id = 1, Name = "Microscopio" });
        db.Context.EquipmentUnits.Add(new EquipmentUnit
        {
            Id = 1,
            EquipmentId = 1,
            InventoryNumber = "UV-001",
            CurrentStatus = EquipmentStatus.Operational
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var page = Prepare(new EditModel(db.Context));
        page.EquipmentUnit = new EditModel.InputModel
        {
            Id = 1,
            EquipmentId = 1,
            InventoryNumber = "UV-001",
            CurrentStatus = EquipmentStatus.OnLoan, // no asignable a mano
            PhysicalCondition = PhysicalCondition.Good
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        var entry = page.ModelState["EquipmentUnit.CurrentStatus"];
        Assert.NotNull(entry);
        Assert.Contains(entry!.Errors, e => e.ErrorMessage.Contains("no puede establecerse manualmente"));
    }

    [Fact]
    public async Task Create_OnPost_LaboratorioAjenoALaFacultad_DevuelvePageConError()
    {
        using var db = new TestDb();
        db.Context.Equipments.Add(new Equipment { Id = 1, Name = "Microscopio" });
        db.Context.Careers.Add(new Career { Id = 10, Name = "Ingeniería Química" });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var page = Prepare(new CreateModel(db.Context, new StubManagementService()));
        page.Input = new CreateModel.InputModel
        {
            FacultyId = 999,               // facultad inexistente
            LaboratoryId = TestDb.LabA,     // laboratorio real de otra facultad (o sin facultad)
            EquipmentId = 1,
            CareerId = 10,
            InventoryNumber = "UV-100",
            CurrentStatus = EquipmentStatus.Operational,
            PhysicalCondition = PhysicalCondition.Good
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        var entry = page.ModelState["Input.LaboratoryId"];
        Assert.NotNull(entry);
        Assert.Contains(entry!.Errors, e => e.ErrorMessage == "El laboratorio seleccionado no pertenece a la facultad indicada.");
    }

    [Fact]
    public async Task Create_OnPost_FabricacionPosteriorAAdquisicion_DevuelvePageConError()
    {
        using var db = new TestDb();
        var page = Prepare(new CreateModel(db.Context, new StubManagementService()));
        page.Input = new CreateModel.InputModel
        {
            FacultyId = 999,
            LaboratoryId = TestDb.LabA,
            EquipmentId = 1,
            CareerId = 10,
            InventoryNumber = "UV-100",
            AcquisitionDate = DateTime.Today.AddDays(-1),
            ManufacturingDate = DateTime.Today // un día después de la adquisición
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        var entry = page.ModelState["Input.ManufacturingDate"];
        Assert.NotNull(entry);
        Assert.Contains(entry!.Errors, e => e.ErrorMessage == "La fecha de fabricación no puede ser posterior a la fecha de adquisición.");
    }

    [Fact]
    public async Task Create_OnPost_AdquisicionAnteriorA1900_DevuelvePageConError()
    {
        using var db = new TestDb();
        var page = Prepare(new CreateModel(db.Context, new StubManagementService()));
        page.Input = new CreateModel.InputModel
        {
            FacultyId = 999,
            LaboratoryId = TestDb.LabA,
            EquipmentId = 1,
            CareerId = 10,
            InventoryNumber = "UV-100",
            AcquisitionDate = new DateTime(1899, 12, 31)
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        var entry = page.ModelState["Input.AcquisitionDate"];
        Assert.NotNull(entry);
        Assert.Contains(entry!.Errors, e => e.ErrorMessage == "La fecha de adquisición debe estar entre 01/01/1900 y la fecha actual.");
    }

    /// <summary>Stub del servicio de gestión: suficiente para constructores de pruebas (no se alcanza en rutas de error).</summary>
    private sealed class StubManagementService : IManagementContextService
    {
        public Task<Management?> GetCurrentManagementAsync(ManagementType? type = null) =>
            Task.FromResult<Management?>(null);

        public void InvalidateCache() { }
    }
}
