using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.Docente;

/// <summary>Calendario mensual/semanal de las prácticas del docente, coloreadas por estado.</summary>
public class CalendarioModel : TeacherPageBase
{
    public CalendarioModel(ApplicationDbContext db, ITeacherRequestService requests) : base(db, requests) { }

    public void OnGet() { }

    /// <summary>Eventos del rango visible (FullCalendar v3 envía start/end).</summary>
    public async Task<IActionResult> OnGetEventsAsync(DateTime start, DateTime end, bool showClosed = false)
    {
        var query = Mine().Where(p => p.ScheduledDate >= start && p.ScheduledDate < end);
        if (!showClosed) query = query.Where(p => p.Status != PlanningStatus.Rejected && p.Status != PlanningStatus.Cancelled);
        var rows = await Project(query).ToListAsync();
        return new JsonResult(rows.Select(r => new
        {
            id = r.Id,
            title = $"{r.LabCode} · {r.Practice}",
            start = (r.Date.Date + (r.Start ?? TimeSpan.Zero)).ToString("yyyy-MM-ddTHH:mm:ss"),
            end = r.End.HasValue ? (r.Date.Date + r.End.Value).ToString("yyyy-MM-ddTHH:mm:ss") : null,
            allDay = !r.Start.HasValue,
            color = TeacherStatus.Color(r.Status),
            textColor = "#ffffff",
            code = L3Codes.Format(r.Id, r.Code),
            practice = r.Practice,
            subject = r.Subject,
            group = r.Group,
            lab = $"{r.LabCode} · {r.LabName}",
            time = r.TimeText,
            periods = r.PeriodsText,
            status = r.StatusText,
            statusCss = r.StatusCss,
            items = r.ItemCount,
            dateText = r.Date.ToString("dddd d 'de' MMMM", new System.Globalization.CultureInfo("es-BO")),
            url = Url.Page("./Solicitud", new { id = r.Id })
        }));
    }
}
