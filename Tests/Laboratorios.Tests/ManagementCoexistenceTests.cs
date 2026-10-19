using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>
/// Coexistencia de gestiones activas (una POR TIPO desde 2026-10-18): la preventiva y la correctiva
/// pueden estar Activas a la vez, pero la correctiva (CORR-AAAA-S) queda fuera de lo académico.
/// La gestión 1 sembrada por TestDb («2026-2») es preventiva y activa; las demás usan Ids 900+.
/// </summary>
public class ManagementCoexistenceTests
{
    private static void SeedManagement(TestDb db, int id, string code, int year, int semester,
        ManagementType type, ManagementStatus status)
    {
        db.Context.Managements.Add(new Management
        {
            Id = id, Code = code, Year = year, Semester = semester, Type = type, Status = status
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task List_ExcludesActiveCorrective_AndKeepsPreventive()
    {
        using var db = new TestDb();
        SeedManagement(db, 901, "CORR-2026-2", 2026, 2, ManagementType.Corrective, ManagementStatus.Active);

        var list = await new AcademicManagementService(db.Context).ListAsync();

        Assert.Contains(list, m => m.Id == TestDb.ManagementId);
        Assert.DoesNotContain(list, m => m.Id == 901);
    }

    [Fact]
    public async Task GetCurrent_PrefersActivePreventive_OverNewerActiveCorrectiveOfSameSemester()
    {
        using var db = new TestDb();
        SeedManagement(db, 901, "CORR-2027-2", 2027, 2, ManagementType.Corrective, ManagementStatus.Active);

        var current = await new AcademicManagementService(db.Context).GetCurrentAsync();

        Assert.NotNull(current);
        Assert.Equal(TestDb.ManagementId, current!.Id);
    }

    [Fact]
    public async Task GetCurrent_IsNull_WhenOnlyTheActiveManagementIsCorrective()
    {
        using var db = new TestDb();
        var preventive = db.Context.Managements.AsTracking().First(m => m.Id == TestDb.ManagementId);
        preventive.Status = ManagementStatus.Completed;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        SeedManagement(db, 901, "CORR-2026-1", 2026, 1, ManagementType.Corrective, ManagementStatus.Active);

        var current = await new AcademicManagementService(db.Context).GetCurrentAsync();

        Assert.Null(current);
    }

    [Fact]
    public async Task Activate_Corrective_ClosesOnlyTheOtherCorrective_AndKeepsPreventiveActive()
    {
        using var db = new TestDb();
        SeedManagement(db, 901, "CORR-2026-1", 2026, 1, ManagementType.Corrective, ManagementStatus.Active);
        SeedManagement(db, 902, "CORR-2026-2", 2026, 2, ManagementType.Corrective, ManagementStatus.Inactive);

        var nueva = db.Context.Managements.AsTracking().First(m => m.Id == 902);
        var result = await new ManagementActivationService(db.Context).ActivateAsync(nueva);
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        Assert.Equal(1, result.ClosedCount);
        Assert.Equal(901, result.ClosedManagementIds.Single());

        var cerrada = db.Context.Managements.AsNoTracking().First(m => m.Id == 901);
        Assert.Equal(ManagementStatus.Completed, cerrada.Status);
        Assert.NotNull(cerrada.ActualClosedDate);

        Assert.Equal(ManagementStatus.Active, db.Context.Managements.AsNoTracking().First(m => m.Id == 902).Status);
        Assert.Equal(ManagementStatus.Active, db.Context.Managements.AsNoTracking().First(m => m.Id == TestDb.ManagementId).Status);
    }

    [Fact]
    public async Task Activate_Preventive_DoesNotCloseActiveCorrective()
    {
        using var db = new TestDb();
        SeedManagement(db, 901, "CORR-2026-1", 2026, 1, ManagementType.Corrective, ManagementStatus.Active);
        SeedManagement(db, 902, "2027-1", 2027, 1, ManagementType.Preventive, ManagementStatus.Inactive);

        var nueva = db.Context.Managements.AsTracking().First(m => m.Id == 902);
        var result = await new ManagementActivationService(db.Context).ActivateAsync(nueva);
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        // Cierra la preventiva activa anterior (Id 1, mismo tipo), jamás la correctiva.
        Assert.DoesNotContain(901, result.ClosedManagementIds);
        Assert.Equal(ManagementStatus.Active, db.Context.Managements.AsNoTracking().First(m => m.Id == 901).Status);
        Assert.Equal(ManagementStatus.Active, db.Context.Managements.AsNoTracking().First(m => m.Id == 902).Status);
    }
}
