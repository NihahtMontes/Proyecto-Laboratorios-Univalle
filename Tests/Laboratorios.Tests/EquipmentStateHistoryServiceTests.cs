using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>
/// Historial de estados como línea de tiempo continua: cada cambio de CurrentStatus cierra el
/// tramo vigente y abre uno nuevo; nunca EndDate &lt; StartDate (CK_EquipmentStateHistories_Dates).
/// </summary>
public class EquipmentStateHistoryServiceTests
{
    private const int EquipmentId = 900;
    private const int UnitId = 901;
    private static readonly DateTime CreatedAt = new(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);

    private static void SeedUnit(TestDb db, EquipmentStatus status = EquipmentStatus.Operational)
    {
        db.Context.Equipments.Add(new Equipment { Id = EquipmentId, Name = "Horno de pruebas" });
        db.Context.EquipmentUnits.Add(new EquipmentUnit
        {
            Id = UnitId,
            EquipmentId = EquipmentId,
            InventoryNumber = "UV-900",
            CurrentStatus = status,
            CreatedDate = CreatedAt
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static EquipmentUnit LoadUnit(TestDb db) =>
        db.Context.EquipmentUnits.AsTracking().Single(u => u.Id == UnitId);

    private static List<EquipmentStateHistory> Rows(TestDb db) =>
        db.Context.EquipmentStateHistories.AsNoTracking()
            .Where(h => h.EquipmentUnitId == UnitId)
            .OrderBy(h => h.StartDate).ThenBy(h => h.Id)
            .ToList();

    [Fact]
    public async Task SinHistorial_CreaTramoDesdeElAltaYElNuevoVigente()
    {
        using var db = new TestDb();
        SeedUnit(db, EquipmentStatus.Operational);
        var service = new EquipmentStateHistoryService(db.Context);
        var at = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

        var unit = LoadUnit(db);
        await service.ChangeStatusAsync(unit, EquipmentStatus.OnLoan, "Préstamo", at);
        db.Context.SaveChanges();

        var rows = Rows(db);
        Assert.Equal(2, rows.Count);
        Assert.Equal(EquipmentStatus.Operational, rows[0].Status);
        Assert.Equal(CreatedAt, rows[0].StartDate);
        Assert.Equal(at, rows[0].EndDate);
        Assert.Equal(EquipmentStatus.OnLoan, rows[1].Status);
        Assert.Equal(at, rows[1].StartDate);
        Assert.Null(rows[1].EndDate);
        Assert.Equal(EquipmentStatus.OnLoan, db.Context.EquipmentUnits.AsNoTracking().Single(u => u.Id == UnitId).CurrentStatus);
    }

    [Fact]
    public async Task CambioDeEstado_CierraLaVigenteYAbreOtra()
    {
        using var db = new TestDb();
        SeedUnit(db);
        var service = new EquipmentStateHistoryService(db.Context);

        var unit = LoadUnit(db);
        await service.EnsureInitialAsync(unit, "Alta inicial");
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var at = new DateTime(2026, 4, 1, 9, 30, 0, DateTimeKind.Utc);
        unit = LoadUnit(db);
        await service.ChangeStatusAsync(unit, EquipmentStatus.UnderMaintenance, "Mantenimiento", at);
        db.Context.SaveChanges();

        var rows = Rows(db);
        Assert.Equal(2, rows.Count);
        Assert.Equal(at, rows[0].EndDate);
        Assert.Equal(EquipmentStatus.UnderMaintenance, rows[1].Status);
        Assert.Equal(at, rows[1].StartDate);
        Assert.Null(rows[1].EndDate);
        Assert.Single(rows, r => r.EndDate == null);
    }

    [Fact]
    public async Task MismoEstado_NoDuplicaLaFila()
    {
        using var db = new TestDb();
        SeedUnit(db);
        var service = new EquipmentStateHistoryService(db.Context);

        var unit = LoadUnit(db);
        await service.EnsureInitialAsync(unit, "Alta inicial");
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        unit = LoadUnit(db);
        var result = await service.ChangeStatusAsync(unit, EquipmentStatus.Operational, "Sin cambio real");
        db.Context.SaveChanges();

        var rows = Rows(db);
        Assert.Single(rows);
        Assert.Equal(rows[0].Id, result.Id);
        Assert.Null(rows[0].EndDate);
    }

    [Fact]
    public async Task SinHistorialYMismoEstado_CreaUnaSolaFilaDesdeElAlta()
    {
        using var db = new TestDb();
        SeedUnit(db);
        var service = new EquipmentStateHistoryService(db.Context);

        var unit = LoadUnit(db);
        await service.ChangeStatusAsync(unit, EquipmentStatus.Operational, "Revisión", new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        db.Context.SaveChanges();

        var row = Assert.Single(Rows(db));
        Assert.Equal(CreatedAt, row.StartDate);
        Assert.Null(row.EndDate);
    }

    [Fact]
    public async Task FechaAnteriorAlInicioVigente_EndDateNuncaMenorQueStartDate()
    {
        using var db = new TestDb();
        SeedUnit(db);
        var openStart = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        db.Context.EquipmentStateHistories.Add(new EquipmentStateHistory
        {
            EquipmentUnitId = UnitId,
            Status = EquipmentStatus.Operational,
            StartDate = openStart
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        var service = new EquipmentStateHistoryService(db.Context);

        // Fecha retroactiva (p. ej. una devolución registrada con fecha pasada).
        var unit = LoadUnit(db);
        await service.ChangeStatusAsync(unit, EquipmentStatus.OnLoan, "Préstamo", openStart.AddDays(-3));
        db.Context.SaveChanges();

        var rows = Rows(db);
        Assert.Equal(2, rows.Count);
        Assert.All(rows.Where(r => r.EndDate.HasValue), r => Assert.True(r.EndDate >= r.StartDate));
        Assert.Equal(openStart, rows[0].EndDate);
        Assert.Equal(openStart, rows[1].StartDate);
    }

    [Fact]
    public async Task VariosCambiosSinGuardar_DejanUnSoloTramoVigenteYContinuo()
    {
        using var db = new TestDb();
        SeedUnit(db);
        var service = new EquipmentStateHistoryService(db.Context);
        var t1 = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
        var t2 = t1.AddDays(2);

        var unit = LoadUnit(db);
        await service.ChangeStatusAsync(unit, EquipmentStatus.UnderMaintenance, "Mantenimiento", t1);
        await service.ChangeStatusAsync(unit, EquipmentStatus.Operational, "Fin de mantenimiento", t2);
        db.Context.SaveChanges();

        var rows = Rows(db);
        Assert.Equal(3, rows.Count);
        Assert.Single(rows, r => r.EndDate == null);
        for (var i = 1; i < rows.Count; i++)
            Assert.Equal(rows[i - 1].EndDate, rows[i].StartDate);
    }

    [Fact]
    public async Task MotivoLargo_SeRecortaA500()
    {
        using var db = new TestDb();
        SeedUnit(db);
        var service = new EquipmentStateHistoryService(db.Context);

        var unit = LoadUnit(db);
        var row = await service.ChangeStatusAsync(unit, EquipmentStatus.Broken, new string('x', 800), userId: 7);

        Assert.Equal(500, row.Reason!.Length);
        Assert.Equal(7, row.CreatedById);
    }
}
