using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.Docente;

/// <summary>Fila de una práctica/solicitud del docente (listas, agenda y calendario).</summary>
public sealed record TeacherPlanningRow(
    int Id,
    string Code,
    DateTime Date,
    TimeSpan? Start,
    TimeSpan? End,
    int? StartOrder,
    int? PeriodCount,
    string? Subject,
    string? Group,
    string Practice,
    string? LabCode,
    string? LabName,
    string Status,
    int ItemCount,
    DateTime RequestDate)
{
    public string TimeText => Start.HasValue ? $"{Start.Value:hh\\:mm}–{(End ?? Start.Value):hh\\:mm}" : "Sin horario";
    public string PeriodsText => StartOrder is int s && PeriodCount is int c ? PlanningSlot.PeriodsText(s, c) : "";
    public string StatusText => TeacherStatus.Text(Status);
    public string StatusCss => TeacherStatus.Css(Status);
    public string StatusIcon => TeacherStatus.Icon(Status);
}

/// <summary>Textos, colores e iconos de estado desde el punto de vista del docente.</summary>
public static class TeacherStatus
{
    public static string Text(string status) => status switch
    {
        PlanningStatus.Requested => "En revisión",
        PlanningStatus.Observed => "Observada",
        PlanningStatus.Rejected => "Rechazada",
        PlanningStatus.Cancelled => "Retirada",
        PlanningStatus.Approved => "Aprobada",
        PlanningStatus.Finalized => "Realizada",
        PlanningStatus.Planned => "Planificada",
        PlanningStatus.Rescheduled or PlanningStatus.Recovered => "Reprogramada",
        PlanningStatus.PendingRecovery => "Por reponer",
        _ => status
    };

    public static string Css(string status) => status switch
    {
        PlanningStatus.Requested => "primary",
        PlanningStatus.Observed => "orange",
        PlanningStatus.Rejected => "danger",
        PlanningStatus.Cancelled => "secondary",
        PlanningStatus.Approved => "success",
        PlanningStatus.Finalized => "info",
        PlanningStatus.Planned => "warning",
        _ => "light"
    };

    public static string Icon(string status) => status switch
    {
        PlanningStatus.Requested => "fas fa-hourglass-half",
        PlanningStatus.Observed => "fas fa-comment-dots",
        PlanningStatus.Rejected => "fas fa-times-circle",
        PlanningStatus.Cancelled => "fas fa-undo",
        PlanningStatus.Approved => "fas fa-check-circle",
        PlanningStatus.Finalized => "fas fa-flag-checkered",
        _ => "fas fa-calendar"
    };

    /// <summary>Color del evento en el calendario (hex) por estado.</summary>
    public static string Color(string status) => status switch
    {
        PlanningStatus.Requested => "#7460ee",
        PlanningStatus.Observed => "#fb8c00",
        PlanningStatus.Rejected => "#9aa0a6",
        PlanningStatus.Cancelled => "#9aa0a6",
        PlanningStatus.Approved => "#36bea6",
        PlanningStatus.Finalized => "#2962ff",
        _ => "#ffbc34"
    };
}

/// <summary>
/// Base de las páginas del portal docente: exige el rol Docente con persona vinculada y expone el perfil
/// y la ventana de solicitud. Todas las consultas se filtran por <see cref="Planning.TeacherPersonId"/>.
/// </summary>
[Authorize(Roles = AuthorizationHelper.RoleTeacher)]
public abstract class TeacherPageBase : PageModel
{
    protected readonly ApplicationDbContext Db;
    protected readonly ITeacherRequestService Requests;

    protected TeacherPageBase(ApplicationDbContext db, ITeacherRequestService requests)
    {
        Db = db;
        Requests = requests;
    }

    public TeacherProfile Teacher { get; private set; } = null!;
    public RequestWindow Window { get; private set; } = null!;

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        var profile = userId > 0 ? await Requests.GetProfileAsync(userId) : null;
        if (profile == null)
        {
            context.Result = RedirectToPage("/AccesoDenegado");
            return;
        }
        Teacher = profile;
        Window = await Requests.GetWindowAsync(DateTime.Today);
        await next();
    }

    /// <summary>Planificaciones del docente (cualquier estado), sin borradas.</summary>
    protected IQueryable<Planning> Mine() =>
        Db.Plannings.AsNoTracking().Where(p => !p.IsDeleted && p.TeacherPersonId == Teacher.PersonId);

    protected static IQueryable<TeacherPlanningRow> Project(IQueryable<Planning> query) =>
        query.Select(p => new TeacherPlanningRow(
            p.Id,
            p.Code ?? "",
            p.ScheduledDate,
            p.StartTime,
            p.EndTime,
            p.StartPeriod != null ? (int?)p.StartPeriod.Order : null,
            p.PeriodCount,
            p.Subject != null ? p.Subject.Name : null,
            p.CourseGroup != null ? p.CourseGroup.Name : p.GroupName,
            p.PracticeName,
            p.Laboratory != null ? p.Laboratory.Code : null,
            p.Laboratory != null ? p.Laboratory.Name : null,
            p.Status,
            p.Requirements.Count,
            p.RequestDate));

    protected static string CodeOf(TeacherPlanningRow row) => L3Codes.Format(row.Id, row.Code);
}
