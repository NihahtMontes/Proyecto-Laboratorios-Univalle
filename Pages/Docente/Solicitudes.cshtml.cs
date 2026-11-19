using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.Docente;

/// <summary>Mis solicitudes: pestañas por estado con búsqueda.</summary>
public class SolicitudesModel : TeacherPageBase
{
    public SolicitudesModel(ApplicationDbContext db, ITeacherRequestService requests) : base(db, requests) { }

    public sealed record Tab(string Key, string Title, string Icon, string Css, string[] Statuses, int Count);

    public static readonly (string Key, string Title, string Icon, string Css, string[] Statuses)[] TabDefs =
    [
        ("revision", "En revisión", "fas fa-hourglass-half", "primary", [PlanningStatus.Requested]),
        ("observadas", "Observadas", "fas fa-comment-dots", "orange", [PlanningStatus.Observed]),
        ("aprobadas", "Aprobadas", "fas fa-check-circle", "success", [PlanningStatus.Approved]),
        ("realizadas", "Realizadas", "fas fa-flag-checkered", "info", [PlanningStatus.Finalized]),
        ("cerradas", "Rechazadas / retiradas", "fas fa-archive", "secondary", [PlanningStatus.Rejected, PlanningStatus.Cancelled])
    ];

    public List<Tab> Tabs { get; private set; } = [];
    public string ActiveTab { get; private set; } = "revision";
    public string? Search { get; private set; }
    public List<TeacherPlanningRow> Rows { get; private set; } = [];
    /// <summary>Último comentario del revisor por solicitud (observación o rechazo).</summary>
    public Dictionary<int, string?> LastComment { get; private set; } = [];

    public async Task OnGetAsync(string? tab, string? q)
    {
        var counts = await Mine().GroupBy(p => p.Status).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        Tabs = TabDefs.Select(t => new Tab(t.Key, t.Title, t.Icon, t.Css, t.Statuses, t.Statuses.Sum(s => counts.GetValueOrDefault(s)))).ToList();

        // Sin pestaña pedida: la primera que requiere atención (observadas) o la primera con datos.
        ActiveTab = Tabs.Any(t => t.Key == tab) ? tab!
            : Tabs.First(t => t.Key == "observadas").Count > 0 ? "observadas"
            : Tabs.FirstOrDefault(t => t.Count > 0)?.Key ?? "revision";
        var statuses = Tabs.First(t => t.Key == ActiveTab).Statuses;
        Search = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        var query = Mine().Where(p => statuses.Contains(p.Status));
        if (Search != null)
        {
            // El código visible "PRA-00013" se deriva del Id: si el texto es de esa forma, buscar también por número.
            var code = Regex.Match(Search, @"^PRA-?0*(\d+)$", RegexOptions.IgnoreCase);
            var planningId = 0;
            var byId = code.Success && int.TryParse(code.Groups[1].Value, out planningId);
            query = query.Where(p => p.PracticeName.Contains(Search) || (p.Code != null && p.Code.Contains(Search))
                || (p.Subject != null && p.Subject.Name.Contains(Search))
                || (byId && p.Id == planningId));
        }
        var ordered = ActiveTab is "aprobadas" or "revision" or "observadas"
            ? query.OrderBy(p => p.ScheduledDate)
            : query.OrderByDescending(p => p.ScheduledDate);
        Rows = await Project(ordered).Take(200).ToListAsync();

        var ids = Rows.Select(r => r.Id).ToList();
        LastComment = (await Db.PlanningReviews.AsNoTracking()
                .Where(r => ids.Contains(r.PlanningId) && r.Comment != null
                    && (r.Action == PlanningReviewAction.Observed || r.Action == PlanningReviewAction.Rejected))
                .Select(r => new { r.PlanningId, r.Comment, r.CreatedDate })
                .ToListAsync())
            .GroupBy(r => r.PlanningId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedDate).First().Comment);
    }
}
