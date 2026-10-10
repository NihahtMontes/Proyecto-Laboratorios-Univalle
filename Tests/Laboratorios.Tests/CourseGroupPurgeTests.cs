using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>
/// Pruebas de la purga (borrado físico aprobado) de grupos de materia: al completar una gestión o
/// al eliminar un grupo manualmente, sus slots se borran, las referencias CourseGroupId de
/// Plannings/PracticeSchedules se sueltan conservando el nombre del grupo en GroupName.
/// </summary>
public class CourseGroupPurgeTests
{
    private const int SubjectId = 500;       // Química General
    private const int GroupId = 600;         // grupo "A" de la gestión 1
    private const int OtherGroupId = 601;    // grupo que debe sobrevivir
    private const int PlanningId = 900;      // planificación de la gestión 1 (PracticeSchedule Id = 9000)
    private const int OtherPlanningId = 901; // planificación ligada al grupo sobreviviente
    private const int ScheduleId = PlanningId * 10;

    [Fact]
    public async Task PurgeManagement_DeletesGroupsAndSlotsOfThatManagementOnly_ReleasesLinksBackfillingGroupName()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroupSlots(GroupId, (1, 1), (2, 2));
        db.AddManagement(TestDb.Management2Id, "2027-1", year: 2027, semester: 1);
        db.AddGroup(OtherGroupId, "QG-2-A", "B", SubjectId, managementId: TestDb.Management2Id);
        db.AddGroupSlots(OtherGroupId, (3, 4));

        // Planificación vinculada al grupo que se purga, con GroupName vacío: debe rellenarse con "A".
        db.AddPlanning(PlanningId, TestDb.Week1Monday, 1, 2, courseGroupId: GroupId);
        SetPlanningLink(db, PlanningId, GroupId, groupName: null);
        SetScheduleLink(db, ScheduleId, GroupId, groupName: null);

        // Planificación de la otra gestión, ligada al grupo sobreviviente: no se toca.
        db.AddPlanning(OtherPlanningId, TestDb.Week1Monday, 3, 2, courseGroupId: OtherGroupId);

        var service = new CourseGroupPurgeService(db.Context);
        var deleted = await service.PurgeManagementAsync(TestDb.ManagementId);

        Assert.Equal(1, deleted);
        // Grupo y slots de la gestión purgada desaparecen.
        Assert.Empty(db.Context.CourseGroups.IgnoreQueryFilters().Where(g => g.ManagementId == TestDb.ManagementId));
        Assert.Empty(db.Context.CourseGroupScheduleSlots.IgnoreQueryFilters().Where(s => s.CourseGroupId == GroupId));
        // El grupo de la otra gestión y sus slots quedan intactos.
        Assert.NotNull(db.Context.CourseGroups.IgnoreQueryFilters().SingleOrDefault(g => g.Id == OtherGroupId));
        Assert.Single(db.Context.CourseGroupScheduleSlots.IgnoreQueryFilters().Where(s => s.CourseGroupId == OtherGroupId));

        // La planificación conserva la materia y el nombre del grupo; el vínculo se suelta.
        var planning = db.Context.Plannings.AsNoTracking().Single(p => p.Id == PlanningId);
        Assert.Null(planning.CourseGroupId);
        Assert.Equal("A", planning.GroupName);
        var schedule = db.Context.PracticeSchedules.AsNoTracking().Single(ps => ps.Id == ScheduleId);
        Assert.Null(schedule.CourseGroupId);
        Assert.Equal("A", schedule.GroupName);

        // La planificación ligada al grupo sobreviviente no cambia.
        var otherPlanning = db.Context.Plannings.AsNoTracking().Single(p => p.Id == OtherPlanningId);
        Assert.Equal(OtherGroupId, otherPlanning.CourseGroupId);
    }

    [Fact]
    public async Task DeleteGroup_RemovesOnlyThatGroup_ReleasesLinksPreservingExistingGroupName()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(OtherGroupId, "QG-1-B", "B", SubjectId);
        db.AddGroupSlots(GroupId, (1, 1));
        db.AddGroupSlots(OtherGroupId, (2, 2));

        // GroupName ya estaba informado: se conserva tal cual (no se sobrescribe).
        db.AddPlanning(PlanningId, TestDb.Week1Monday, 1, 2, courseGroupId: GroupId);
        db.AddPlanning(OtherPlanningId, TestDb.Week1Monday, 3, 2, courseGroupId: OtherGroupId);

        var service = new CourseGroupPurgeService(db.Context);
        var deleted = await service.DeleteGroupAsync(GroupId);

        Assert.Equal(1, deleted);
        Assert.Null(db.Context.CourseGroups.IgnoreQueryFilters().SingleOrDefault(g => g.Id == GroupId));
        Assert.Empty(db.Context.CourseGroupScheduleSlots.IgnoreQueryFilters().Where(s => s.CourseGroupId == GroupId));
        Assert.NotNull(db.Context.CourseGroups.IgnoreQueryFilters().SingleOrDefault(g => g.Id == OtherGroupId));
        Assert.Single(db.Context.CourseGroupScheduleSlots.IgnoreQueryFilters().Where(s => s.CourseGroupId == OtherGroupId));

        var planning = db.Context.Plannings.AsNoTracking().Single(p => p.Id == PlanningId);
        Assert.Null(planning.CourseGroupId);
        Assert.Equal("A", planning.GroupName);
        var otherPlanning = db.Context.Plannings.AsNoTracking().Single(p => p.Id == OtherPlanningId);
        Assert.Equal(OtherGroupId, otherPlanning.CourseGroupId);
    }

    [Fact]
    public async Task DeleteGroup_UnknownGroup_DeletesNothing()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);

        var service = new CourseGroupPurgeService(db.Context);
        var deleted = await service.DeleteGroupAsync(99999);

        Assert.Equal(0, deleted);
        Assert.NotNull(db.Context.CourseGroups.IgnoreQueryFilters().SingleOrDefault(g => g.Id == GroupId));
    }

    [Fact]
    public async Task PurgeDeleted_RemovesOnlyEliminadoGroups_AndReleasesTheirLinks()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(OtherGroupId, "QG-1-X", "X", SubjectId, status: GeneralStatus.Eliminado);   // legado soft-delete
        db.AddGroupSlots(OtherGroupId, (4, 5));

        // Planificación ligada al grupo Eliminado: su vínculo se suelta rellenando el nombre.
        db.AddPlanning(PlanningId, TestDb.Week1Monday, 1, 2, courseGroupId: OtherGroupId);
        SetPlanningLink(db, PlanningId, OtherGroupId, groupName: null);
        // Planificación ligada al grupo activo: no se toca.
        db.AddPlanning(OtherPlanningId, TestDb.Week1Monday, 3, 2, courseGroupId: GroupId);

        var service = new CourseGroupPurgeService(db.Context);
        var deleted = await service.PurgeDeletedAsync();

        Assert.Equal(1, deleted);
        Assert.Null(db.Context.CourseGroups.IgnoreQueryFilters().SingleOrDefault(g => g.Id == OtherGroupId));
        Assert.Empty(db.Context.CourseGroupScheduleSlots.IgnoreQueryFilters().Where(s => s.CourseGroupId == OtherGroupId));
        Assert.NotNull(db.Context.CourseGroups.IgnoreQueryFilters().SingleOrDefault(g => g.Id == GroupId));

        var planning = db.Context.Plannings.AsNoTracking().Single(p => p.Id == PlanningId);
        Assert.Null(planning.CourseGroupId);
        Assert.Equal("X", planning.GroupName);
        var otherPlanning = db.Context.Plannings.AsNoTracking().Single(p => p.Id == OtherPlanningId);
        Assert.Equal(GroupId, otherPlanning.CourseGroupId);
    }

    // ==================== Helpers de vínculo (EF InMemory: carga con AsTracking y modifica) ====================

    private static void SetPlanningLink(TestDb db, int planningId, int courseGroupId, string? groupName)
    {
        var planning = db.Context.Plannings.AsTracking().Single(p => p.Id == planningId);
        planning.CourseGroupId = courseGroupId;
        planning.GroupName = groupName;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static void SetScheduleLink(TestDb db, int scheduleId, int courseGroupId, string? groupName)
    {
        var schedule = db.Context.PracticeSchedules.AsTracking().Single(ps => ps.Id == scheduleId);
        schedule.CourseGroupId = courseGroupId;
        schedule.GroupName = groupName;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }
}
