using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using DeparturesCreateModel = Proyecto_Laboratorios_Univalle.Pages.Departures.CreateModel;
using RequestsCreateModel = Proyecto_Laboratorios_Univalle.Pages.Requests.CreateModel;
using VerificationsCreateModel = Proyecto_Laboratorios_Univalle.Pages.Verifications.CreateModel;
using VerificationsEditModel = Proyecto_Laboratorios_Univalle.Pages.Verifications.EditModel;

namespace Laboratorios.Tests;

/// <summary>
/// QA R-02: los formularios de fases tempranas del wizard (L-6 individual, L-7 y L-3 individual)
/// deben validar la fase del plan antes de guardar. Crear o editar con el plan en otra fase no
/// puede mover el wizard hacia atrás ni saltarse fases; los registros sueltos (sin plan) y el
/// flujo legítimo L-6 → L-7 → L-8 → L-3 → Kardex → L-12 deben seguir funcionando igual.
/// </summary>
public class WizardPhaseGuardEarlyPhasesTests
{
    private const int PlanId = 50;
    private const int VerificationId = 20;
    private const int RequestId = 77;
    private const int UnitId = 1;

    /// <summary>
    /// Inicializa PageContext (ViewData + HttpContext) y TempData: fuera del pipeline MVC ambos
    /// son null y las rutas de retorno escriben en ViewData / TempData.Warning.
    /// </summary>
    private static TPage Prepare<TPage>(TPage page) where TPage : PageModel
    {
        var httpContext = new DefaultHttpContext();
        page.PageContext = new PageContext
        {
            HttpContext = httpContext,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
        page.TempData = new TempDataDictionary(httpContext, new EmptyTempDataProvider());
        return page;
    }

    private static UserManager<User> NoOpUserManager() =>
        new(new NoOpUserStore(), null!, null!, null!, null!, null!, null!, null!,
            NullLogger<UserManager<User>>.Instance);

    private static string Checklist(int items = 13) =>
        string.Join("\r\n", Enumerable.Range(1, items).Select(i => $"{i}. Punto de control técnico {i}."));

    private static void SeedUnit(TestDb db)
    {
        db.Context.Equipments.Add(new Equipment { Id = 1, Name = "Microscopio de pruebas" });
        db.Context.EquipmentUnits.Add(new EquipmentUnit
        {
            Id = UnitId,
            EquipmentId = 1,
            InventoryNumber = "UV-T-001",
            LaboratoryId = TestDb.LabA,
            CurrentStatus = EquipmentStatus.Operational
        });
    }

    private static void AddPlan(TestDb db, WizardPhase phase, WizardEquipmentState state,
        int? requestId = null, int? verificationId = null)
    {
        db.Context.ManagementPlans.Add(new ManagementPlan
        {
            Id = PlanId,
            ManagementId = TestDb.ManagementId,
            EquipmentUnitId = UnitId,
            CurrentPhase = phase,
            CurrentState = state,
            PlanStatus = ManagementPlanStatus.InProgress,
            RequestId = requestId,
            VerificationId = verificationId
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static ManagementPlan GetPlan(TestDb db)
    {
        db.Context.ChangeTracker.Clear();
        return db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
    }

    // ═══════════════ L-7 Requests/Create ═══════════════

    [Fact]
    public async Task Requests_Create_OnPost_PlanEnFaseMantenimiento_NoCreaSolicitudNiCambiaPlan()
    {
        using var db = new TestDb();
        SeedUnit(db);
        AddPlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance);

        var page = Prepare(new RequestsCreateModel(db.Context, NoOpUserManager(), new StubManagementService(),
            NullLogger<RequestsCreateModel>.Instance));
        page.ManagementPlanId = PlanId;
        page.Input = new RequestsCreateModel.InputModel
        {
            Description = "No arranca el motor",
            Observations = Checklist(),
            TechnicalChecklistConfirmed = true
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(0, db.Context.Requests.Count());
        var plan = GetPlan(db);
        Assert.Equal(WizardPhase.Maintenance, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingMaintenance, plan.CurrentState);
        Assert.Null(plan.RequestId);
        Assert.Contains("L-7 Solicitud técnica", Assert.IsType<string>(page.TempData["WarningMessage"]));
    }

    [Fact]
    public async Task Requests_Create_OnGet_PlanEnOtraFase_RedirigeAlWizardSinMostrarFormulario()
    {
        using var db = new TestDb();
        SeedUnit(db);
        AddPlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance);

        var page = Prepare(new RequestsCreateModel(db.Context, NoOpUserManager(), new StubManagementService(),
            NullLogger<RequestsCreateModel>.Instance));
        page.ManagementPlanId = PlanId;

        var redirect = Assert.IsType<RedirectToPageResult>(await page.OnGetAsync());

        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal((int)WizardPhase.Maintenance, (int)redirect.RouteValues["Step"]!);
        Assert.Equal(TestDb.ManagementId, (int)redirect.RouteValues["ManagementId"]!);
        Assert.Contains("L-7 Solicitud técnica", Assert.IsType<string>(page.TempData["WarningMessage"]));
    }

    [Fact]
    public async Task Requests_Create_OnPost_PlanEnFaseSolicitud_RegistraL7YAvanzaAMantenimiento()
    {
        using var db = new TestDb();
        SeedUnit(db);
        AddPlan(db, WizardPhase.TechnicalRequest, WizardEquipmentState.AwaitingRequest);

        var page = Prepare(new RequestsCreateModel(db.Context, NoOpUserManager(), new StubManagementService(),
            NullLogger<RequestsCreateModel>.Instance));
        page.ManagementPlanId = PlanId;
        page.Input = new RequestsCreateModel.InputModel
        {
            Description = "Falla de refrigeración",
            Observations = Checklist(),
            TechnicalChecklistConfirmed = true
        };

        var redirect = Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());

        Assert.Equal("./Index", redirect.PageName);
        var request = db.Context.Requests.AsNoTracking().Single();
        Assert.Equal(RequestStatus.Pending, request.Status);
        Assert.Equal(RequestType.Technical, request.Type);
        Assert.Equal(TestDb.ManagementId, request.ManagementId);
        var plan = GetPlan(db);
        Assert.Equal(WizardPhase.Maintenance, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingMaintenance, plan.CurrentState);
        Assert.Equal(request.Id, plan.RequestId);
        Assert.NotNull(page.TempData["SuccessMessage"]);
    }

    // ═══════════════ L-6 Verifications/Create ═══════════════

    [Fact]
    public async Task Verifications_Create_OnPost_PlanEnOtraFase_NoGuardaVerificacionNiCambiaPlan()
    {
        using var db = new TestDb();
        SeedUnit(db);
        AddPlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance);

        var page = Prepare(new VerificationsCreateModel(db.Context, NoOpUserManager(), new StubManagementService()));
        page.ManagementPlanId = PlanId;
        page.ManagementId = TestDb.ManagementId;
        page.Input = new VerificationsCreateModel.InputModel
        {
            FacultyId = 10,
            LaboratoryId = TestDb.LabA,
            EquipmentUnitId = UnitId,
            Date = new DateTime(2026, 10, 10)
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(0, db.Context.Verifications.Count());
        var plan = GetPlan(db);
        Assert.Equal(WizardPhase.Maintenance, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingMaintenance, plan.CurrentState);
        Assert.Contains("L-6 Verificación", Assert.IsType<string>(page.TempData["WarningMessage"]));
    }

    [Fact]
    public async Task Verifications_Create_OnGet_PlanEnOtraFase_RedirigeAlWizardSinMostrarFormulario()
    {
        using var db = new TestDb();
        SeedUnit(db);
        AddPlan(db, WizardPhase.Exit, WizardEquipmentState.AwaitingDeparture);

        var page = Prepare(new VerificationsCreateModel(db.Context, NoOpUserManager(), new StubManagementService()));
        page.ManagementPlanId = PlanId;

        var redirect = Assert.IsType<RedirectToPageResult>(await page.OnGetAsync());

        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal((int)WizardPhase.Exit, (int)redirect.RouteValues["Step"]!);
        Assert.Contains("L-6 Verificación", Assert.IsType<string>(page.TempData["WarningMessage"]));
    }

    // ═══════════════ L-3 Departures/Create ═══════════════

    [Fact]
    public async Task Departures_Create_OnPost_PlanEnOtraFase_NoGuardaSalidaNiCambiaPlan()
    {
        using var db = new TestDb();
        SeedUnit(db);
        AddPlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance);

        var page = Prepare(new DeparturesCreateModel(db.Context, NoOpUserManager(), new StubManagementService(),
            NullLogger<DeparturesCreateModel>.Instance));
        page.ManagementPlanId = PlanId;
        page.Input = new DeparturesCreateModel.InputModel
        {
            EquipmentUnitId = UnitId,
            BorrowerId = 5,
            DepartureDate = new DateTime(2026, 10, 10),
            EstimatedReturnDate = new DateTime(2026, 10, 25)
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(0, db.Context.Departures.Count());
        var plan = GetPlan(db);
        Assert.Equal(WizardPhase.Maintenance, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingMaintenance, plan.CurrentState);
        Assert.Contains("L-3 Salida", Assert.IsType<string>(page.TempData["WarningMessage"]));
    }

    [Fact]
    public async Task Departures_Create_OnGet_PlanEnOtraFase_RedirigeAlWizardSinMostrarFormulario()
    {
        using var db = new TestDb();
        SeedUnit(db);
        AddPlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance);

        var page = Prepare(new DeparturesCreateModel(db.Context, NoOpUserManager(), new StubManagementService(),
            NullLogger<DeparturesCreateModel>.Instance));

        var redirect = Assert.IsType<RedirectToPageResult>(
            await page.OnGetAsync(equipmentUnitId: null, isWizard: true, managementPlanId: PlanId));

        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal((int)WizardPhase.Maintenance, (int)redirect.RouteValues["Step"]!);
        Assert.Equal(TestDb.ManagementId, (int)redirect.RouteValues["ManagementId"]!);
        Assert.Contains("L-3 Salida", Assert.IsType<string>(page.TempData["WarningMessage"]));
    }

    // ═══════════════ L-6 Verifications/Edit (ventana temprana) ═══════════════

    private static void SeedVerificationWithPlan(TestDb db, WizardPhase phase, WizardEquipmentState state, int? requestId = null)
    {
        SeedUnit(db);
        db.Context.Verifications.Add(new Verification
        {
            Id = VerificationId,
            EquipmentUnitId = UnitId,
            ManagementId = TestDb.ManagementId,
            Date = new DateTime(2026, 10, 5),
            Observations = "Observación original",
            PhysicalCondition = PhysicalCondition.Bad,
            Status = VerificationStatus.WithObservations
        });
        if (requestId.HasValue)
        {
            db.Context.Requests.Add(new Request
            {
                Id = RequestId,
                Type = RequestType.Technical,
                Status = RequestStatus.Pending,
                ManagementId = TestDb.ManagementId,
                EquipmentUnitId = UnitId
            });
        }
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        AddPlan(db, phase, state, requestId, VerificationId);
    }

    private static VerificationsEditModel.EditInputModel HealthyInput() => new()
    {
        Id = VerificationId,
        EquipmentUnitId = UnitId,
        Date = new DateTime(2026, 10, 10),
        Observations = "Ajuste menor, sin fallas",
        PhysicalCondition = PhysicalCondition.Good,
        Status = VerificationStatus.Completed
    };

    [Fact]
    public async Task Verifications_Edit_OnPost_PlanEnFaseSalida_ActualizaVerificacionSinMoverPlan()
    {
        using var db = new TestDb();
        SeedVerificationWithPlan(db, WizardPhase.Exit, WizardEquipmentState.AwaitingDeparture);

        var page = Prepare(new VerificationsEditModel(db.Context, NoOpUserManager()));
        page.Input = HealthyInput();

        var redirect = Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());

        Assert.Equal("./Index", redirect.PageName);
        db.Context.ChangeTracker.Clear();
        var verification = db.Context.Verifications.AsNoTracking().Single();
        Assert.Equal("Ajuste menor, sin fallas", verification.Observations);
        Assert.Equal(VerificationStatus.Completed, verification.Status);
        Assert.NotNull(verification.LastModifiedDate);
        var plan = GetPlan(db);
        Assert.Equal(WizardPhase.Exit, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingDeparture, plan.CurrentState);
        Assert.Null(plan.LastModifiedDate); // el plan no fue tocado
        var warning = Assert.IsType<string>(page.TempData["WarningMessage"]);
        Assert.Contains("el plan no se modificó", warning);
        Assert.Contains("L-3 Salida", warning);
    }

    [Fact]
    public async Task Verifications_Edit_OnPost_PlanConL7Registrado_NoCancelaSolicitudNiMuevePlan()
    {
        using var db = new TestDb();
        SeedVerificationWithPlan(db, WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance, RequestId);

        var page = Prepare(new VerificationsEditModel(db.Context, NoOpUserManager()));
        page.Input = HealthyInput();

        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());

        db.Context.ChangeTracker.Clear();
        var request = db.Context.Requests.AsNoTracking().Single();
        Assert.Equal(RequestStatus.Pending, request.Status); // no se canceló
        var plan = GetPlan(db);
        Assert.Equal(WizardPhase.Maintenance, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingMaintenance, plan.CurrentState);
        Assert.Equal(RequestId, plan.RequestId);
        Assert.Contains("el plan no se modificó", Assert.IsType<string>(page.TempData["WarningMessage"]));
    }

    [Fact]
    public async Task Verifications_Edit_OnPost_VentanaTemprana_MueveElPlanAActivosSanos()
    {
        using var db = new TestDb();
        SeedVerificationWithPlan(db, WizardPhase.TechnicalRequest, WizardEquipmentState.AwaitingRequest);

        var page = Prepare(new VerificationsEditModel(db.Context, NoOpUserManager()));
        page.Input = HealthyInput();

        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());

        var plan = GetPlan(db);
        Assert.Equal(WizardPhase.Verification, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.VerifiedGood, plan.CurrentState);
        Assert.NotNull(page.TempData["SuccessMessage"]);
        Assert.Null(page.TempData["WarningMessage"]);
    }

    // ═══════════════ Lógica extraída: CanUpdateLinkedPlanPhase ═══════════════

    [Fact]
    public void CanUpdate_PlanNulo_DevuelveFalse()
    {
        Assert.False(VerificationsEditModel.CanUpdateLinkedPlanPhase(null));
    }

    [Fact]
    public async Task Verifications_Edit_OnPost_VentanaTemprana_ConSolicitudPendienteAutomatica_CorrigeAActivoSano()
    {
        // Caso real: un L-6 «Malo» deja el plan en TechnicalRequest/AwaitingRequest con una Request Pendiente
        // creada automáticamente. Corregir la verificación a «Bueno» debe seguir funcionando.
        using var db = new TestDb();
        SeedVerificationWithPlan(db, WizardPhase.TechnicalRequest, WizardEquipmentState.AwaitingRequest, RequestId);

        var page = Prepare(new VerificationsEditModel(db.Context, NoOpUserManager()));
        page.Input = HealthyInput();

        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());

        db.Context.ChangeTracker.Clear();
        var plan = GetPlan(db);
        Assert.Equal(WizardPhase.Verification, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.VerifiedGood, plan.CurrentState);
        Assert.Null(page.TempData["WarningMessage"]);
        Assert.NotNull(page.TempData["SuccessMessage"]);
    }

    [Fact]
    public void CanUpdate_FaseVerification_PermiteMoverElPlan()
    {
        Assert.True(VerificationsEditModel.CanUpdateLinkedPlanPhase(new ManagementPlan
        {
            CurrentPhase = WizardPhase.Verification,
            CurrentState = WizardEquipmentState.PendingVerification
        }));
        Assert.True(VerificationsEditModel.CanUpdateLinkedPlanPhase(new ManagementPlan
        {
            CurrentPhase = WizardPhase.Verification,
            CurrentState = WizardEquipmentState.VerifiedGood
        }));
    }

    [Fact]
    public void CanUpdate_SolicitudTecnicaSinL7_PermiteMoverElPlan()
    {
        Assert.True(VerificationsEditModel.CanUpdateLinkedPlanPhase(new ManagementPlan
        {
            CurrentPhase = WizardPhase.TechnicalRequest,
            CurrentState = WizardEquipmentState.AwaitingRequest,
            RequestId = null
        }));
    }

    [Fact]
    public void CanUpdate_SolicitudTecnicaConSolicitudPendienteAutomatica_PermiteMoverElPlan()
    {
        // El L-6 con falla crea una Request Pendiente (RequestId ≠ null) y deja el plan en AwaitingRequest.
        Assert.True(VerificationsEditModel.CanUpdateLinkedPlanPhase(new ManagementPlan
        {
            CurrentPhase = WizardPhase.TechnicalRequest,
            CurrentState = WizardEquipmentState.AwaitingRequest,
            RequestId = RequestId
        }));
    }

    [Fact]
    public void CanUpdate_FasesAvanzadas_NiegaMoverElPlan()
    {
        Assert.False(VerificationsEditModel.CanUpdateLinkedPlanPhase(new ManagementPlan
        {
            CurrentPhase = WizardPhase.Maintenance,
            CurrentState = WizardEquipmentState.AwaitingMaintenance
        }));
        Assert.False(VerificationsEditModel.CanUpdateLinkedPlanPhase(new ManagementPlan
        {
            CurrentPhase = WizardPhase.Exit,
            CurrentState = WizardEquipmentState.AwaitingDeparture
        }));
        Assert.False(VerificationsEditModel.CanUpdateLinkedPlanPhase(new ManagementPlan
        {
            CurrentPhase = WizardPhase.Kardex,
            CurrentState = WizardEquipmentState.AwaitingKardex
        }));
        Assert.False(VerificationsEditModel.CanUpdateLinkedPlanPhase(new ManagementPlan
        {
            CurrentPhase = WizardPhase.Disbursement,
            CurrentState = WizardEquipmentState.AwaitingDisbursement
        }));
    }

    [Fact]
    public void CanUpdate_PlanCompletado_NiegaMoverElPlan()
    {
        Assert.False(VerificationsEditModel.CanUpdateLinkedPlanPhase(new ManagementPlan
        {
            CurrentPhase = WizardPhase.Disbursement,
            CurrentState = WizardEquipmentState.Completed
        }));
        Assert.False(VerificationsEditModel.CanUpdateLinkedPlanPhase(new ManagementPlan
        {
            CurrentPhase = WizardPhase.Verification,
            CurrentState = WizardEquipmentState.VerifiedGood,
            PlanStatus = ManagementPlanStatus.Completed
        }));
    }

    // ═══════════════ Stubs ═══════════════

    private sealed class StubManagementService : IManagementContextService
    {
        public Task<Management?> GetCurrentManagementAsync(ManagementType? type = null) =>
            Task.FromResult<Management?>(null);

        public void InvalidateCache() { }
    }

    private sealed class NoOpUserStore : IUserStore<User>
    {
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
        public void Dispose() { }
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?>? LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?>? values) { }
    }
}
