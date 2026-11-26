using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.Users;
using Xunit;

namespace Laboratorios.Tests;

/// <summary>
/// Vínculo cuenta ↔ docente (Usuarios › Crear/Editar). Regresión del 25/11/2026: no había forma de registrar un docente
/// nuevo desde la web, y la lista decía «3 grupo(s)» de un docente cuyos grupos eran todos de una gestión inactiva.
/// </summary>
public class TeacherLinkTests
{
    private const int Upcoming = 2;           // 2027-1, inactiva (la gestión actual de TestDb es la 1, 2026-2)
    private const int WithCurrent = 300;      // 2 grupos en 2026-2 y 1 en 2027-1
    private const int OnlyUpcoming = 301;     // 3 grupos solo en 2027-1
    private const int WithoutGroups = 302;
    private const int WithAccount = 303;      // docente que ya tiene cuenta
    private const int NotTeacher = 304;       // persona de otra categoría

    private static TestDb Seed()
    {
        var db = new TestDb();
        var c = db.Context;
        c.Managements.Add(new Management
        {
            Id = Upcoming, Code = "2027-1", Year = 2027, Semester = 1, Type = ManagementType.Preventive, Status = ManagementStatus.Inactive
        });
        c.Interns.AddRange(
            Teacher(WithCurrent, "Ana Docente"),
            Teacher(OnlyUpcoming, "Bruno Futuro"),
            Teacher(WithoutGroups, "Carla Nueva"),
            Teacher(WithAccount, "Diego Con Cuenta"),
            new Intern { Id = NotTeacher, Name = "Tecnico Uno", Category = PersonCategory.Tecnico, Status = GeneralStatus.Activo, InternStatus = GeneralStatus.Activo });
        var groupId = 500;
        void Group(int teacher, int management) => c.CourseGroups.Add(new CourseGroup
        {
            Id = groupId++, Code = $"G{groupId}", Name = "A", SubjectId = 1, ManagementId = management, TeacherPersonId = teacher
        });
        Group(WithCurrent, TestDb.ManagementId);
        Group(WithCurrent, TestDb.ManagementId);
        Group(WithCurrent, Upcoming);
        Group(OnlyUpcoming, Upcoming);
        Group(OnlyUpcoming, Upcoming);
        Group(OnlyUpcoming, Upcoming);
        c.Users.Add(new User { Id = 900, UserName = "diego", PersonId = WithAccount, Role = UserRole.Docente });
        c.SaveChanges();
        c.ChangeTracker.Clear();
        return db;
    }

    private static Intern Teacher(int id, string name) => new()
    {
        Id = id, Name = name, Category = PersonCategory.Docente, Status = GeneralStatus.Activo, InternStatus = GeneralStatus.Activo
    };

    [Fact]
    public async Task La_lista_cuenta_los_grupos_de_la_gestion_actual_aparte_de_las_otras()
    {
        using var db = Seed();

        var options = await TeacherLink.OptionsAsync(db.Context, currentUserId: null);

        var ana = Assert.Single(options, o => o.PersonId == WithCurrent);
        Assert.Equal((2, 1, "2026-2"), (ana.CurrentGroups, ana.OtherGroups, ana.CurrentCode));
        Assert.Equal("Ana Docente · 2 grupo(s) en 2026-2 y 1 en otras gestiones", ana.Label);

        var alberto = Assert.Single(options, o => o.PersonId == OnlyUpcoming);
        Assert.Equal("Bruno Futuro · sin grupos en 2026-2 (3 en otras gestiones)", alberto.Label);

        var carla = Assert.Single(options, o => o.PersonId == WithoutGroups);
        Assert.Equal("Carla Nueva · sin grupos asignados", carla.Label);

        // Fuera de la lista: el que ya tiene cuenta y quien no es docente.
        Assert.DoesNotContain(options, o => o.PersonId == WithAccount);
        Assert.DoesNotContain(options, o => o.PersonId == NotTeacher);
    }

    [Fact]
    public async Task Al_editar_el_docente_es_obligatorio_y_al_crear_se_puede_registrar_uno_nuevo()
    {
        using var db = Seed();

        Assert.NotNull(await TeacherLink.ValidateAsync(db.Context, UserRole.Docente, null, null));
        Assert.Null(await TeacherLink.ValidateAsync(db.Context, UserRole.Docente, null, null, allowNew: true));
        Assert.Null(await TeacherLink.ValidateAsync(db.Context, UserRole.Docente, WithoutGroups, null, allowNew: true));
        Assert.NotNull(await TeacherLink.ValidateAsync(db.Context, UserRole.Docente, WithAccount, null, allowNew: true));
        // Los demás roles no llevan docente.
        Assert.Null(await TeacherLink.ValidateAsync(db.Context, UserRole.PracticeClosure, null, null));
    }

    [Fact]
    public async Task No_registra_dos_veces_a_un_docente_que_ya_esta_en_el_catalogo()
    {
        using var db = Seed();

        var sinCuenta = await TeacherLink.NewTeacherErrorAsync(db.Context, "Carla Nueva");
        Assert.Contains("elígelo en la lista", sinCuenta);

        var conCuenta = await TeacherLink.NewTeacherErrorAsync(db.Context, "Diego Con Cuenta");
        Assert.Contains("otra cuenta vinculada", conCuenta);

        Assert.Null(await TeacherLink.NewTeacherErrorAsync(db.Context, "Juan Perez Nuevo"));
        // Un técnico con el mismo nombre no impide registrar al docente.
        Assert.Null(await TeacherLink.NewTeacherErrorAsync(db.Context, "Tecnico Uno"));
        Assert.Contains("100 caracteres", await TeacherLink.NewTeacherErrorAsync(db.Context, new string('a', 101)));
    }

    [Fact]
    public void La_ficha_nueva_queda_como_docente_activo_con_el_nombre_completo()
    {
        Assert.Equal("Juan Perez Lopez", TeacherLink.FullName("  Juan ", "Perez", " Lopez "));
        Assert.Equal("Juan Perez", TeacherLink.FullName("Juan", "Perez", "  "));

        var teacher = TeacherLink.NewTeacher("Juan Perez Lopez", "juan@doc.univalle.edu", "70000001", createdById: 7);

        Assert.Equal(PersonCategory.Docente, teacher.Category);
        Assert.Equal(GeneralStatus.Activo, teacher.Status);
        Assert.Equal(GeneralStatus.Activo, teacher.InternStatus);
        Assert.Equal(("juan@doc.univalle.edu", "70000001", 7), (teacher.Email, teacher.PhoneNumber, teacher.CreatedById));
        // Un correo más largo que la columna de la ficha no se copia (la cuenta lo conserva).
        Assert.Null(TeacherLink.NewTeacher("X", new string('a', 101), null, null).Email);
    }
}
