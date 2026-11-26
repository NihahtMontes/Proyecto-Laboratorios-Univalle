using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;
using Proyecto_Laboratorios_Univalle.Services.StudentPractice;

namespace Laboratorios.Tests;

/// <summary>
/// Práctica libre de estudiantes (decisiones del usuario 2026-11-23): correo institucional, cuentas aprobadas,
/// cupos donde el laboratorio está libre, máximo 2 reservas por semana, cancelar hasta 24 h antes, faltas y suspensión.
/// </summary>
public class StudentPracticeServiceTests
{
    private const int StudentUser = 900;
    private const int OtherUser = 901;
    private const int Admin = 1;
    private static readonly DateTime Now = TestDb.Week1Monday.AddDays(-3).AddHours(10);   // viernes anterior a la semana 1

    private static StudentPracticeService Service(TestDb db, DateTime? now = null) =>
        new(db.Context, new OccupancyService([new PlanningOccupancySource(db.Context), new FreePracticeSlotOccupancySource(db.Context)]),
            () => now ?? Now);

    private static int AddStudent(TestDb db, int userId, StudentAccountStatus status = StudentAccountStatus.Approved, DateTime? suspendedUntil = null)
    {
        db.Context.Users.Add(new User
        {
            Id = userId, UserName = $"e{userId}@est.univalle.edu", Email = $"e{userId}@est.univalle.edu", PhoneNumber = "71234567",
            FirstName = "Est", LastName = $"N{userId}", IdentityCard = $"E{userId}", Role = UserRole.Estudiante,
            Status = status == StudentAccountStatus.Approved ? GeneralStatus.Activo : GeneralStatus.Inactivo
        });
        var profile = new StudentProfile { UserId = userId, Career = "LGS", Semester = 5, AccountStatus = status, SuspendedUntil = suspendedUntil };
        db.Context.StudentProfiles.Add(profile);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        return profile.Id;
    }

    private static async Task<int> Slot(TestDb db, DateTime date, int startOrder = 2, int count = 4, int capacity = 6, int lab = TestDb.LabA)
    {
        var result = await Service(db).CreateSlotAsync(lab, date, startOrder, count, capacity, null, Admin);
        Assert.True(result.Ok, result.Message);
        return result.Id!.Value;
    }

    [Theory]
    [InlineData("ana.perez@est.univalle.edu", true)]
    [InlineData("ANA@EST.UNIVALLE.EDU", true)]
    [InlineData("ana@univalle.edu", false)]
    [InlineData("ana@gmail.com", false)]
    [InlineData("ana@est.univalle.edu.bo", false)]
    [InlineData("a@b@est.univalle.edu", false)]
    public void OnlyInstitutionalStudentEmails(string email, bool ok) => Assert.Equal(ok, StudentAccess.IsInstitutionalEmail(email));

    [Fact]
    public async Task Slot_CannotOverlapAPlanning_AndThenOccupiesTheLaboratory()
    {
        using var db = new TestDb();
        var tuesday = TestDb.Week1Monday.AddDays(1);
        db.AddPlanning(10, tuesday, 1, 4);   // LAB-A P1–P4

        var clash = await Service(db).CreateSlotAsync(TestDb.LabA, tuesday, 3, 2, 6, null, Admin);
        Assert.False(clash.Ok);
        Assert.Contains("ocupado", clash.Message);

        await Slot(db, tuesday, startOrder: 5, count: 4);   // contiguo: permitido (limpieza de 10 min)
        var occupancy = new OccupancyService([new PlanningOccupancySource(db.Context), new FreePracticeSlotOccupancySource(db.Context)]);
        var conflicts = await occupancy.FindConflictsAsync(TestDb.LabA, tuesday, 6, 2, null);
        Assert.Contains(conflicts, c => c.Source == FreePracticeSlotOccupancySource.SourceName);
    }

    [Fact]
    public async Task Reserve_RequiresApprovedAccount_AndNotSuspended()
    {
        using var db = new TestDb();
        AddStudent(db, StudentUser, StudentAccountStatus.Pending);
        AddStudent(db, OtherUser, suspendedUntil: Now.AddDays(10));
        var slot = await Slot(db, TestDb.Week1Monday.AddDays(1));

        Assert.False((await Service(db).ReserveAsync(StudentUser, slot, "Masas", [])).Ok);
        var suspended = await Service(db).ReserveAsync(OtherUser, slot, "Masas", []);
        Assert.False(suspended.Ok);
        Assert.Contains("suspendido", suspended.Message);
    }

    [Fact]
    public async Task Reserve_AtMostTwoPerWeek_NoDuplicates_AndNotWhenFull()
    {
        using var db = new TestDb();
        AddStudent(db, StudentUser);
        AddStudent(db, OtherUser);
        var monday = TestDb.Week1Monday;
        var a = await Slot(db, monday.AddDays(1));
        var b = await Slot(db, monday.AddDays(2));
        var c = await Slot(db, monday.AddDays(3));
        var full = await Slot(db, monday.AddDays(4), capacity: 1);
        var nextWeek = await Slot(db, TestDb.Week2Monday.AddDays(1));

        Assert.True((await Service(db).ReserveAsync(StudentUser, a, "Salsas", [])).Ok);
        Assert.False((await Service(db).ReserveAsync(StudentUser, a, "Salsas", [])).Ok);   // duplicada
        Assert.True((await Service(db).ReserveAsync(StudentUser, b, "Masas", [])).Ok);
        var third = await Service(db).ReserveAsync(StudentUser, c, "Postres", []);
        Assert.False(third.Ok);
        Assert.Contains("2 reservas", third.Message);
        Assert.True((await Service(db).ReserveAsync(StudentUser, nextWeek, "Postres", [])).Ok);   // otra semana

        Assert.True((await Service(db).ReserveAsync(OtherUser, full, "Cortes", [])).Ok);
        Assert.False((await Service(db).ReserveAsync(StudentUser, full, "Cortes", [])).Ok);   // lleno (la pendiente ocupa lugar)
    }

    [Fact]
    public async Task Cancel_OnlyUpTo24HoursBefore()
    {
        using var db = new TestDb();
        AddStudent(db, StudentUser);
        var tuesday = TestDb.Week1Monday.AddDays(1);
        var slot = await Slot(db, tuesday);   // P2 = 08:35
        var reservation = (await Service(db).ReserveAsync(StudentUser, slot, "Salsas", [])).Id!.Value;

        var late = await Service(db, tuesday.AddHours(-1)).CancelReservationAsync(StudentUser, reservation);   // lunes 23:00
        Assert.False(late.Ok);
        Assert.True((await Service(db).CancelReservationAsync(StudentUser, reservation)).Ok);
        Assert.Equal(SlotReservationStatus.Cancelled, db.Context.SlotReservations.AsNoTracking().Single().Status);
    }

    [Fact]
    public async Task Approval_RespectsCapacity_AndNoShowLeavesAnIncident()
    {
        using var db = new TestDb();
        AddStudent(db, StudentUser);
        AddStudent(db, OtherUser);
        var tuesday = TestDb.Week1Monday.AddDays(1);
        var slot = await Slot(db, tuesday, capacity: 2);
        var r1 = (await Service(db).ReserveAsync(StudentUser, slot, "Salsas", [])).Id!.Value;
        var r2 = (await Service(db).ReserveAsync(OtherUser, slot, "Masas", [])).Id!.Value;

        Assert.True((await Service(db).ReviewReservationAsync(r1, true, null, Admin)).Ok);
        Assert.False((await Service(db).ReviewReservationAsync(r2, false, "", Admin)).Ok);   // rechazo sin motivo
        Assert.True((await Service(db).ReviewReservationAsync(r2, false, "No trae menú", Admin)).Ok);

        Assert.False((await Service(db).MarkAttendanceAsync(r1, SlotReservationStatus.NoShow, null, Admin)).Ok);   // todavía no es el día
        Assert.True((await Service(db, tuesday.AddHours(13)).MarkAttendanceAsync(r1, SlotReservationStatus.NoShow, null, Admin)).Ok);
        var incident = Assert.Single(db.Context.StudentIncidents.AsNoTracking());
        Assert.Equal(StudentIncidentType.NoShow, incident.Type);
        // Corregir la asistencia no duplica la falta.
        Assert.True((await Service(db, tuesday.AddHours(13)).MarkAttendanceAsync(r1, SlotReservationStatus.Misuse, "Rompió un bowl", Admin)).Ok);
        Assert.Single(db.Context.StudentIncidents.AsNoTracking());
        Assert.Equal(2, db.Context.Notifications.AsNoTracking().Count(n => n.Scope == "PracticaLibre"));   // aprobada y rechazada
    }

    [Fact]
    public async Task Deactivate_InactivatesTheAccount_AndCancelsFutureReservations()
    {
        using var db = new TestDb();
        var profile = AddStudent(db, StudentUser);
        var slot = await Slot(db, TestDb.Week1Monday.AddDays(1));
        await Service(db).ReserveAsync(StudentUser, slot, "Salsas", []);

        Assert.False((await Service(db).DeactivateAsync(profile, "")).Ok);
        Assert.True((await Service(db).DeactivateAsync(profile, "Reserva falsa reiterada")).Ok);

        Assert.Equal(GeneralStatus.Inactivo, db.Context.Users.AsNoTracking().Single(u => u.Id == StudentUser).Status);
        Assert.Equal(SlotReservationStatus.Cancelled, db.Context.SlotReservations.AsNoTracking().Single().Status);
    }
}
