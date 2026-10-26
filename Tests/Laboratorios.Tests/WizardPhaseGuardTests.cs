using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Notifications;
using AcquisitionCreateModel = Proyecto_Laboratorios_Univalle.Pages.Acquisitions.CreateModel;
using KardexCreateModel = Proyecto_Laboratorios_Univalle.Pages.Kardex.CreateModel;
using MaintenanceCreateModel = Proyecto_Laboratorios_Univalle.Pages.Maintenances.CreateModel;

namespace Laboratorios.Tests;

/// <summary>
/// Guarda de fase del wizard (QA R-02): los formularios de L-8, Kardex y L-12 solo deben
/// operar cuando el plan está en SU fase; en otro caso se rechaza sin crear registros ni
/// mover el plan. Incluye las reglas de avance: L-8 solo pasa el plan a L-3 cuando queda
/// Completado (100% + al menos una tarea completada, validado en servidor), el borrador de
/// Kardex no reabre un L-8 Completado y L-12 solo cierra el plan desde Desembolso.
/// </summary>
public class WizardPhaseGuardTests
{
    private const int EquipmentId = 700;
    private const int UnitId = 710;
    private const int MaintenanceId = 730;
    private const int DepartureId = 740;
    private const int KardexHistoryId = 750;
    private const int PlanId = 760;

    private static readonly DateTime MaintenanceStart = new(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MaintenanceEnd = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DepartureDate = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReturnDate = new(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc);

    // ═══════════════ (a) WizardPhaseGuard.Validate ═══════════════

    [Fact]
    public void Validate_PlanNulo_PermiteElRegistroSuelto()
    {
        Assert.Null(WizardPhaseGuard.Validate(null, WizardPhase.Maintenance));
        Assert.Null(WizardPhaseGuard.Validate(null, WizardPhase.Kardex));
        Assert.Null(WizardPhaseGuard.Validate(null, WizardPhase.Disbursement));
    }

    [Theory]
    [InlineData(WizardPhase.Verification)]
    [InlineData(WizardPhase.TechnicalRequest)]
    [InlineData(WizardPhase.Maintenance)]
    [InlineData(WizardPhase.Exit)]
    [InlineData(WizardPhase.Kardex)]
    [InlineData(WizardPhase.Disbursement)]
    public void Validate_PlanEnLaMismaFase_Permite(WizardPhase phase)
    {
        var plan = NewPlan(phase, WizardEquipmentState.PendingVerification);

        Assert.Null(WizardPhaseGuard.Validate(plan, phase));
    }

    [Theory]
    [InlineData(WizardPhase.Verification, WizardPhase.Maintenance)]
    [InlineData(WizardPhase.TechnicalRequest, WizardPhase.Maintenance)]
    [InlineData(WizardPhase.Exit, WizardPhase.Maintenance)]
    [InlineData(WizardPhase.Kardex, WizardPhase.Maintenance)]
    [InlineData(WizardPhase.Verification, WizardPhase.Kardex)]
    [InlineData(WizardPhase.Maintenance, WizardPhase.Kardex)]
    [InlineData(WizardPhase.Kardex, WizardPhase.Disbursement)]
    [InlineData(WizardPhase.Maintenance, WizardPhase.Disbursement)]
    public void Validate_PlanEnOtraFase_RechazaConElNombreDeAmbasFases(WizardPhase actual, WizardPhase expected)
    {
        var plan = NewPlan(actual, WizardEquipmentState.PendingVerification);

        var error = WizardPhaseGuard.Validate(plan, expected);

        Assert.NotNull(error);
        Assert.Contains(WizardPhaseGuard.PhaseLabel(expected), error);
        Assert.Contains(WizardPhaseGuard.PhaseLabel(actual), error);
        Assert.Contains("No se registró nada", error);
    }

    [Fact]
    public void Validate_PlanCompletadoPorEstado_RechazaCualquierFase()
    {
        foreach (var expected in new[] { WizardPhase.Verification, WizardPhase.TechnicalRequest, WizardPhase.Maintenance, WizardPhase.Exit, WizardPhase.Kardex, WizardPhase.Disbursement })
        {
            var plan = NewPlan(WizardPhase.Disbursement, WizardEquipmentState.Completed);

            var error = WizardPhaseGuard.Validate(plan, expected);

            Assert.NotNull(error);
            Assert.Contains("completó su ronda de mantenimiento", error);
            Assert.Contains(WizardPhaseGuard.PhaseLabel(expected), error);
        }
    }

    [Fact]
    public void Validate_PlanCompletadoPorPlanStatus_RechazaAunqueLaFaseCoincida()
    {
        var plan = NewPlan(WizardPhase.Kardex, WizardEquipmentState.AwaitingKardex, ManagementPlanStatus.Completed);

        Assert.NotNull(WizardPhaseGuard.Validate(plan, WizardPhase.Kardex));
    }

    [Theory]
    [InlineData(WizardPhase.Verification, "Verificación de equipos")]
    [InlineData(WizardPhase.TechnicalRequest, "Solicitud de servicio técnico")]
    [InlineData(WizardPhase.Maintenance, "Registro de mantenimiento")]
    [InlineData(WizardPhase.Exit, "Salida de equipos")]
    [InlineData(WizardPhase.Kardex, "Cierre del mantenimiento")]
    [InlineData(WizardPhase.Disbursement, "Solicitud de compra")]
    public void PhaseLabel_DevuelveNombresVisiblesEnEspanol(WizardPhase phase, string expected)
    {
        Assert.Equal(expected, WizardPhaseGuard.PhaseLabel(phase));
    }

    // ═══════════════ (b) L-8 Maintenances/Create ═══════════════

    [Fact]
    public async Task MaintenanceCreate_PlanEnOtraFase_NoGuardaNadaNiMueveElPlan()
    {
        using var db = new TestDb();
        SeedMaintenancePlan(db, WizardPhase.Exit, WizardEquipmentState.AwaitingDeparture);
        var page = MaintenanceCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input = ValidCompletedInput();

        var result = await page.OnPostAsync(isWizard: true);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal((int)WizardPhase.Exit, Convert.ToInt32(redirect.RouteValues!["Step"]));
        Assert.Equal(TestDb.ManagementId, Convert.ToInt32(redirect.RouteValues!["ManagementId"]));
        Assert.NotNull(page.TempData["WarningMessage"]);

        Assert.Equal(0, db.Context.Maintenances.Count());
        Assert.Equal(0, db.Context.EquipmentStateHistories.Count());
        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.Exit, plan.CurrentPhase);
        Assert.Null(plan.MaintenanceId);
        Assert.False(plan.IsDraft);
    }

    [Fact]
    public async Task MaintenanceCreate_CompletadoAl100ConTecnico_AvanzaElPlanASalida()
    {
        using var db = new TestDb();
        SeedMaintenancePlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance);
        var page = MaintenanceCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input = ValidCompletedInput();

        var result = await page.OnPostAsync(isWizard: true);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal(4, Convert.ToInt32(redirect.RouteValues!["Step"])); // L-3 Salida

        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.Exit, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingDeparture, plan.CurrentState);
        Assert.False(plan.IsDraft);
        Assert.NotNull(plan.MaintenanceId);
        Assert.NotNull(plan.KardexHistoryId);

        var maintenance = db.Context.Maintenances.AsNoTracking().Single(m => m.Id == plan.MaintenanceId);
        Assert.Equal(MaintenanceStatus.Completed, maintenance.Status);
        Assert.Equal(100, maintenance.CompletionPercentage);
    }

    [Fact]
    public async Task MaintenanceCreate_Pendiente_GuardaPeroDejaElPlanEnMantenimiento()
    {
        using var db = new TestDb();
        SeedMaintenancePlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance);
        var page = MaintenanceCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input = ValidCompletedInput();
        page.Input.Status = MaintenanceStatus.Pending;
        page.Input.StartDate = null; // aún no empezó
        page.Input.EndDate = null;
        page.Input.Tasks = new List<MaintenanceTask>
        {
            new() { Description = "Limpieza", IsCompleted = true },
            new() { Description = "Calibración", IsCompleted = false }
        };

        var result = await page.OnPostAsync(isWizard: true);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(3, Convert.ToInt32(redirect.RouteValues!["Step"])); // sigue en L-8

        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.Maintenance, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingMaintenance, plan.CurrentState);
        Assert.NotNull(plan.MaintenanceId);

        var maintenance = db.Context.Maintenances.AsNoTracking().Single(m => m.Id == plan.MaintenanceId);
        Assert.Equal(MaintenanceStatus.Pending, maintenance.Status);
    }

    [Fact]
    public async Task MaintenanceCreate_CompletadoConAvanceIncompleto_SeRechazaEnServidor()
    {
        using var db = new TestDb();
        SeedMaintenancePlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance);
        var page = MaintenanceCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input = ValidCompletedInput();
        page.Input.CompletionPercentage = 100; // el cliente miente: solo 1 de 2 tareas completa
        page.Input.Tasks = new List<MaintenanceTask>
        {
            new() { Description = "Limpieza", IsCompleted = true },
            new() { Description = "Calibración", IsCompleted = false }
        };

        var result = await page.OnPostAsync(isWizard: true);

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        var entry = page.ModelState["Input.CompletionPercentage"];
        Assert.NotNull(entry);
        Assert.Contains(entry!.Errors, e => e.ErrorMessage.Contains("el avance debe estar al 100%"));
        Assert.Contains(entry.Errors, e => e.ErrorMessage.Contains("al menos una tarea completada"));

        Assert.Equal(0, db.Context.Maintenances.Count());
        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.Maintenance, plan.CurrentPhase);
        Assert.Null(plan.MaintenanceId);
    }

    [Fact]
    public async Task MaintenanceCreate_CompletadoSinTareas_SeRechazaEnServidor()
    {
        using var db = new TestDb();
        SeedMaintenancePlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance);
        var page = MaintenanceCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input = ValidCompletedInput();
        page.Input.Tasks = new List<MaintenanceTask>();

        var result = await page.OnPostAsync(isWizard: true);

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        var entry = page.ModelState["Input.CompletionPercentage"];
        Assert.NotNull(entry);
        Assert.Contains(entry!.Errors, e => e.ErrorMessage.Contains("al menos una tarea completada"));
        Assert.Equal(0, db.Context.Maintenances.Count());
    }

    [Fact]
    public async Task MaintenanceCreate_DefinitivoSinTecnico_SeRechaza()
    {
        using var db = new TestDb();
        SeedMaintenancePlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance);
        var page = MaintenanceCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input = ValidCompletedInput();
        page.Input.TechnicianId = null;

        var result = await page.OnPostAsync(isWizard: true);

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        var entry = page.ModelState["Input.TechnicianId"];
        Assert.NotNull(entry);
        Assert.Contains(entry!.Errors, e => e.ErrorMessage.Contains("El técnico responsable es obligatorio"));
        Assert.Equal(0, db.Context.Maintenances.Count());
    }

    // ═══════════════ (b) Kardex/Create ═══════════════

    [Fact]
    public async Task KardexClose_PlanEnOtraFase_NoCreaHistorialNiMueveElPlan()
    {
        using var db = new TestDb();
        SeedClosedPlan(db, WizardPhase.Disbursement, WizardEquipmentState.AwaitingDisbursement);
        var page = KardexCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input.EquipmentUnitId = UnitId;

        var result = await page.OnPostCloseAsync(isWizard: true);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal((int)WizardPhase.Disbursement, Convert.ToInt32(redirect.RouteValues!["Step"]));
        Assert.NotNull(page.TempData["WarningMessage"]);

        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.Disbursement, plan.CurrentPhase);
        Assert.Equal(MaintenanceId, plan.MaintenanceId);
        Assert.Equal(KardexHistoryId, plan.KardexHistoryId);

        // Nada de historial nuevo ni reescritura del existente.
        var histories = db.Context.EquipmentStateHistories.AsNoTracking().ToList();
        var seeded = histories.Single(h => h.Id == KardexHistoryId);
        Assert.Null(seeded.EndDate);
        Assert.Equal(EquipmentStatus.Operational, seeded.Status);

        var maintenance = db.Context.Maintenances.AsNoTracking().Single(m => m.Id == MaintenanceId);
        Assert.Equal(MaintenanceStatus.Completed, maintenance.Status);
    }

    [Fact]
    public async Task KardexDraft_PlanEnOtraFase_NoReabreElMantenimientoNiMarcaBorrador()
    {
        using var db = new TestDb();
        SeedClosedPlan(db, WizardPhase.Disbursement, WizardEquipmentState.AwaitingDisbursement);
        var page = KardexCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input.EquipmentUnitId = UnitId;

        var result = await page.OnPostDraftAsync(isWizard: true);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.NotNull(page.TempData["WarningMessage"]);

        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.Disbursement, plan.CurrentPhase);
        Assert.False(plan.IsDraft);
        Assert.Null(plan.DraftPhase);

        var maintenance = db.Context.Maintenances.AsNoTracking().Single(m => m.Id == MaintenanceId);
        Assert.Equal(MaintenanceStatus.Completed, maintenance.Status);
    }

    [Fact]
    public async Task KardexDraft_PlanEnFaseKardex_GuardaBorradorSinReabrirElL8Completado()
    {
        using var db = new TestDb();
        SeedClosedPlan(db, WizardPhase.Kardex, WizardEquipmentState.AwaitingKardex);
        var page = KardexCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input.EquipmentUnitId = UnitId;

        var result = await page.OnPostDraftAsync(isWizard: true);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal(5, Convert.ToInt32(redirect.RouteValues!["Step"]));

        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.True(plan.IsDraft);
        Assert.Equal(WizardPhase.Kardex, plan.DraftPhase);
        Assert.Equal(WizardPhase.Kardex, plan.CurrentPhase);

        var maintenance = db.Context.Maintenances.AsNoTracking().Single(m => m.Id == MaintenanceId);
        Assert.Equal(MaintenanceStatus.Completed, maintenance.Status);
    }

    // ═══════════════ (b) Acquisitions/Create (L-12) ═══════════════

    [Fact]
    public async Task AcquisitionCreate_PlanEnOtraFase_NoCreaSolicitudNiCompletaElPlan()
    {
        using var db = new TestDb();
        SeedPlanWithoutRequest(db, WizardPhase.Kardex, WizardEquipmentState.AwaitingKardex);
        var page = AcquisitionCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input = ValidAcquisitionInput();

        var result = await page.OnPostAsync(isWizard: true);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal((int)WizardPhase.Kardex, Convert.ToInt32(redirect.RouteValues!["Step"]));
        Assert.NotNull(page.TempData["WarningMessage"]);

        Assert.Equal(0, db.Context.Requests.Count(r => r.Type == RequestType.Purchasing));
        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.Kardex, plan.CurrentPhase);
        Assert.Equal(ManagementPlanStatus.InProgress, plan.PlanStatus);
        Assert.Null(plan.AcquisitionRequestId);
    }

    [Fact]
    public async Task AcquisitionCreate_PlanEnDesembolso_RegistraSolicitudYCompletaElPlan()
    {
        using var db = new TestDb();
        SeedPlanWithoutRequest(db, WizardPhase.Disbursement, WizardEquipmentState.AwaitingDisbursement);
        var page = AcquisitionCreatePage(db);
        page.ManagementPlanId = PlanId;
        page.Input = ValidAcquisitionInput();

        var result = await page.OnPostAsync(isWizard: true);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal(7, Convert.ToInt32(redirect.RouteValues!["Step"])); // Completados

        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(ManagementPlanStatus.Completed, plan.PlanStatus);
        Assert.Equal(WizardEquipmentState.Completed, plan.CurrentState);
        Assert.NotNull(plan.AcquisitionRequestId);

        var request = db.Context.Requests.AsNoTracking().Single(r => r.Type == RequestType.Purchasing);
        Assert.Equal(UnitId, request.EquipmentUnitId);
    }

    // ═══════════════ Siembra y helpers ═══════════════

    private static ManagementPlan NewPlan(WizardPhase phase, WizardEquipmentState state,
        ManagementPlanStatus status = ManagementPlanStatus.InProgress) => new()
    {
        Id = PlanId,
        ManagementId = TestDb.ManagementId,
        CurrentPhase = phase,
        CurrentState = state,
        PlanStatus = status
    };

    private static void SeedUnit(TestDb db)
    {
        db.Context.Equipments.Add(new Equipment { Id = EquipmentId, Name = "Microscopio de pruebas" });
        db.Context.EquipmentUnits.Add(new EquipmentUnit
        {
            Id = UnitId,
            EquipmentId = EquipmentId,
            InventoryNumber = "UV-700",
            LaboratoryId = TestDb.LabA,
            CurrentStatus = EquipmentStatus.Operational
        });
    }

    /// <summary>Plan en fase L-8 (o posterior) sin mantenimiento vinculado aún.</summary>
    private static void SeedMaintenancePlan(TestDb db, WizardPhase phase, WizardEquipmentState state)
    {
        SeedUnit(db);
        db.Context.ManagementPlans.Add(new ManagementPlan
        {
            Id = PlanId,
            ManagementId = TestDb.ManagementId,
            EquipmentUnitId = UnitId,
            CurrentPhase = phase,
            CurrentState = state,
            PlanStatus = ManagementPlanStatus.InProgress
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    /// <summary>Plan con L-8 Completado, L-3 devuelta e historial Kardex abierto (fase configurable).</summary>
    private static void SeedClosedPlan(TestDb db, WizardPhase phase, WizardEquipmentState state)
    {
        SeedUnit(db);
        db.Context.Maintenances.Add(new Maintenance
        {
            Id = MaintenanceId,
            EquipmentUnitId = UnitId,
            ManagementId = TestDb.ManagementId,
            Status = MaintenanceStatus.Completed,
            CompletionPercentage = 100,
            StartDate = MaintenanceStart,
            EndDate = MaintenanceEnd
        });
        db.Context.Departures.Add(new Departure
        {
            Id = DepartureId,
            ManagementId = TestDb.ManagementId,
            DepartureDate = DepartureDate,
            Status = LoanStatus.Returned,
            ActualReturnDate = ReturnDate
        });
        db.Context.EquipmentStateHistories.Add(new EquipmentStateHistory
        {
            Id = KardexHistoryId,
            EquipmentUnitId = UnitId,
            Status = EquipmentStatus.Operational,
            StartDate = ReturnDate,
            EndDate = null
        });
        db.Context.ManagementPlans.Add(new ManagementPlan
        {
            Id = PlanId,
            ManagementId = TestDb.ManagementId,
            EquipmentUnitId = UnitId,
            MaintenanceId = MaintenanceId,
            DepartureId = DepartureId,
            KardexHistoryId = KardexHistoryId,
            CurrentPhase = phase,
            CurrentState = state,
            PlanStatus = ManagementPlanStatus.InProgress
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    /// <summary>Plan en fase Kardex/Desembolso sin solicitud de adquisición vinculada.</summary>
    private static void SeedPlanWithoutRequest(TestDb db, WizardPhase phase, WizardEquipmentState state)
    {
        SeedUnit(db);
        db.Context.ManagementPlans.Add(new ManagementPlan
        {
            Id = PlanId,
            ManagementId = TestDb.ManagementId,
            EquipmentUnitId = UnitId,
            CurrentPhase = phase,
            CurrentState = state,
            PlanStatus = ManagementPlanStatus.InProgress
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static MaintenanceCreateModel.InputModel ValidCompletedInput() => new()
    {
        Status = MaintenanceStatus.Completed,
        TechnicianId = TestDb.Teacher1,
        ScheduledDate = MaintenanceStart.Date,
        StartDate = MaintenanceStart,
        EndDate = MaintenanceEnd,
        Description = "Mantenimiento preventivo de prueba",
        CompletionPercentage = 100,
        Tasks = new List<MaintenanceTask>
        {
            new() { Description = "Limpieza y desinfección", IsCompleted = true },
            new() { Description = "Calibración y ajustes", IsCompleted = true }
        }
    };

    private static AcquisitionCreateModel.InputModel ValidAcquisitionInput() => new()
    {
        Description = "Repuestos para el mantenimiento preventivo",
        InvestmentCode = "MIC-001",
        CostCenter = "LAB-A",
        Observations = "Solicitud generada desde el wizard"
    };

    /// <summary>
    /// Inicializa PageContext con ViewData/ModelState y un HttpContext mínimo, más un
    /// TempData en memoria: los handlers escriben ViewData (LoadLists) y TempData en sus
    /// rutas de rechazo. RedirectToPage no necesita UrlHelper para construirse.
    /// </summary>
    private static TPage Prepare<TPage>(TPage page) where TPage : PageModel
    {
        page.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext(),
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
        page.TempData = new TempDataDictionary(page.HttpContext, new StubTempDataProvider());
        return page;
    }

    private static MaintenanceCreateModel MaintenanceCreatePage(TestDb db) => Prepare(
        new MaintenanceCreateModel(
            db.Context,
            new UserManager<User>(new StubUserStore(), null!, null!, null!, null!, null!, null!, null!, NullLogger<UserManager<User>>.Instance),
            new StubManagementService(),
            new StubNotificationWriter(),
            NullLogger<MaintenanceCreateModel>.Instance,
            new EquipmentStateHistoryService(db.Context)));

    private static KardexCreateModel KardexCreatePage(TestDb db) => Prepare(
        new KardexCreateModel(db.Context, NullLogger<KardexCreateModel>.Instance, new EquipmentStateHistoryService(db.Context)));

    private static AcquisitionCreateModel AcquisitionCreatePage(TestDb db) => Prepare(
        new AcquisitionCreateModel(
            db.Context,
            new UserManager<User>(new StubUserStore(), null!, null!, null!, null!, null!, null!, null!, NullLogger<UserManager<User>>.Instance),
            new StubManagementService(),
            NullLogger<AcquisitionCreateModel>.Instance));

    private sealed class StubTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class StubManagementService : IManagementContextService
    {
        public Task<Management?> GetCurrentManagementAsync(ManagementType? type = null) => Task.FromResult<Management?>(null);
        public void InvalidateCache() { }
    }

    private sealed class StubNotificationWriter : IMaintenanceNotificationWriter
    {
        public Task NotifyAssignmentAsync(int maintenanceId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>UserStore mínimo: solo se necesita GetUserAsync, que devuelve null sin claims.</summary>
    private sealed class StubUserStore : IUserStore<User>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
        public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.UserName);
        public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);
        public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<User?>(null);
    }
}
