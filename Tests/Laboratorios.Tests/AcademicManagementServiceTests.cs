using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>
/// Gestión por defecto de la planificación. Con los datos importados todas las gestiones son «Por confirmar»;
/// antes solo contaba la preventiva activa y las pantallas de planificación abrían en «No hay una gestión académica activa».
/// </summary>
public class AcademicManagementServiceTests
{
    [Fact]
    public async Task GetCurrent_UsesActivePreventiveManagement()
    {
        using var db = new TestDb();   // la gestión 1 (2026-2) es preventiva y está activa

        var current = await new AcademicManagementService(db.Context).GetCurrentAsync();

        Assert.NotNull(current);
        Assert.Equal(TestDb.ManagementId, current!.Id);
    }

    [Fact]
    public async Task GetCurrent_FallsBackToActiveManagementOfAnyType_WhenNoPreventiveIsActive()
    {
        using var db = new TestDb();
        var preventive = db.Context.Managements.First(m => m.Id == TestDb.ManagementId);
        preventive.Status = ManagementStatus.Completed;
        db.Context.Managements.Update(preventive);
        db.Context.Managements.Add(new Management
        {
            Id = 2, Code = "2026-3", Year = 2026, Semester = 2, Type = ManagementType.PorConfirmar, Status = ManagementStatus.Active
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var current = await new AcademicManagementService(db.Context).GetCurrentAsync();

        Assert.NotNull(current);
        Assert.Equal(2, current!.Id);
        Assert.True(current.IsCurrent);
    }

    [Fact]
    public async Task GetCurrent_PrefersPreventiveOverNewerActiveManagementOfAnotherType()
    {
        using var db = new TestDb();
        db.Context.Managements.Add(new Management
        {
            Id = 2, Code = "2027-1", Year = 2027, Semester = 1, Type = ManagementType.PorConfirmar, Status = ManagementStatus.Active
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var current = await new AcademicManagementService(db.Context).GetCurrentAsync();

        Assert.Equal(TestDb.ManagementId, current!.Id);
    }

    [Fact]
    public async Task GetCurrent_IsNull_WhenNoSemesterManagementIsActive()
    {
        using var db = new TestDb();
        var management = db.Context.Managements.First(m => m.Id == TestDb.ManagementId);
        management.Status = ManagementStatus.Completed;
        db.Context.Managements.Update(management);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        Assert.Null(await new AcademicManagementService(db.Context).GetCurrentAsync());
    }
}
