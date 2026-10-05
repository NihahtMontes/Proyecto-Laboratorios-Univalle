using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

namespace Proyecto_Laboratorios_Univalle.Pages.Cierre;

/// <summary>
/// Cierre de práctica: prácticas del día (y las de días anteriores con devolución pendiente) para registrar
/// la entrega y devolución de material (L-3) y los incidentes (L-5). Proceso aparte de la planificación.
/// El encargado de cierre solo ve el día de hoy; la gestión puede consultar otra fecha.
/// </summary>
[Authorize(Roles = AuthorizationHelper.ClosureRoles)]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context) => _context = context;

    [BindProperty(SupportsGet = true)]
    public DateTime? Date { get; set; }

    public DateTime SelectedDate { get; private set; }
    public bool IsClosureOnly { get; private set; }
    public bool IsToday => SelectedDate == DateTime.Today;

    public List<ClosureRow> Practices { get; private set; } = [];
    public List<ClosureRow> PendingReturns { get; private set; } = [];

    public int ToDeliverCount => Practices.Count(p => p.Stage == ClosureStage.ToDeliver);
    public int ToReturnCount => Practices.Count(p => p.Stage == ClosureStage.ToReturn) + PendingReturns.Count;
    public int ClosedCount => Practices.Count(p => p.Stage == ClosureStage.Closed);

    public enum ClosureStage
    {
        /// <summary>Planificación sin aprobar: todavía no se puede entregar.</summary>
        NotApproved,
        /// <summary>Aprobada, sin salida de material.</summary>
        ToDeliver,
        /// <summary>Material entregado, devolución pendiente.</summary>
        ToReturn,
        /// <summary>Material devuelto, falta finalizar la práctica.</summary>
        ToFinalize,
        /// <summary>Práctica finalizada.</summary>
        Closed
    }

    public sealed record ClosureRow(
        int Id, DateTime Date, TimeSpan? StartTime, TimeSpan? EndTime, string PracticeName, string? SubjectName,
        string? GroupName, string? LaboratoryCode, string? TeacherName, string Status, int ItemCount,
        bool HasDeparture, bool HasOpenDeparture, int IncidentCount)
    {
        public string Code => L3Codes.Format(Id);

        public string TimeText => StartTime.HasValue
            ? $"{StartTime.Value:hh\\:mm}–{(EndTime ?? StartTime.Value):hh\\:mm}"
            : "Sin horario";

        public ClosureStage Stage =>
            Status == L3RequestStatus.Finalized ? ClosureStage.Closed
            : Status != L3RequestStatus.Approved ? ClosureStage.NotApproved
            : !HasDeparture ? ClosureStage.ToDeliver
            : HasOpenDeparture ? ClosureStage.ToReturn
            : ClosureStage.ToFinalize;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        IsClosureOnly = PracticeClosureAccess.IsClosureOnly(User);
        // El encargado de cierre trabaja solo con el día de hoy.
        SelectedDate = IsClosureOnly || !Date.HasValue ? DateTime.Today : Date.Value.Date;

        var day = SelectedDate;
        var next = day.AddDays(1);
        Practices = await Project(_context.L3Requests
                .Where(r => r.ScheduledDate >= day && r.ScheduledDate < next && r.Status != L3RequestStatus.Cancelled))
            .ToListAsync();
        Practices = Practices.OrderBy(p => p.StartTime ?? TimeSpan.MaxValue).ThenBy(p => p.Id).ToList();

        // Otros días con material sin devolver (anteriores o adelantados): también se cierran desde aquí.
        var openStatuses = new[] { LoanStatus.Active, LoanStatus.Overdue };
        PendingReturns = (await Project(_context.L3Requests
                    .Where(r => (r.ScheduledDate < day || r.ScheduledDate >= next) && r.Status == L3RequestStatus.Approved
                        && _context.Departures.Any(d => d.L3RequestId == r.Id && openStatuses.Contains(d.Status))))
                .ToListAsync())
            .OrderBy(p => p.Date).ThenBy(p => p.StartTime ?? TimeSpan.MaxValue)
            .ToList();

        return Page();
    }

    /// <summary>Proyección común de una práctica planificada para el cierre.</summary>
    private IQueryable<ClosureRow> Project(IQueryable<L3Request> source)
    {
        var openStatuses = new[] { LoanStatus.Active, LoanStatus.Overdue };
        return source.AsNoTracking()
            .Select(r => new ClosureRow(
                r.Id,
                r.ScheduledDate,
                r.StartTime,
                r.EndTime,
                r.PracticeName,
                r.Subject != null ? r.Subject.Name : null,
                r.CourseGroup != null ? r.CourseGroup.Name : r.GroupName,
                r.Laboratory != null ? r.Laboratory.Code : null,
                _context.Interns.Where(i => i.Id == r.TeacherPersonId).Select(i => i.Name).FirstOrDefault(),
                r.Status,
                r.Details.Count,
                _context.Departures.Any(d => d.L3RequestId == r.Id && d.Status != LoanStatus.Cancelled),
                _context.Departures.Any(d => d.L3RequestId == r.Id && openStatuses.Contains(d.Status)),
                r.Incidents.Count));
    }
}
