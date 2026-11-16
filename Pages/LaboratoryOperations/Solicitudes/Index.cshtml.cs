using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations.Solicitudes;

/// <summary>
/// Bandeja de solicitudes de docentes (Supervisor / Administrador / SuperAdmin): por urgencia, con banderas de
/// choque, competencia y material; permite aprobar en lote las que no tienen ningún aviso.
/// </summary>
[Authorize(Roles = AuthorizationHelper.ReviewerRoles)]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ITeacherRequestService _requests;

    public IndexModel(ApplicationDbContext context, ITeacherRequestService requests)
    {
        _context = context;
        _requests = requests;
    }

    public sealed record InboxRow(
        int Id, string Code, string Teacher, string Practice, string? Subject, string? Group, DateTime Date, TimeSpan? Start, TimeSpan? End,
        string? LabCode, int Items, string Status, DateTime RequestDate, int Blocks, int Competitors, int MaterialShort, int MaterialNoStock,
        string? Comment, DateTime? LastActionDate = null, bool Withdrawn = false)
    {
        /// <summary>Estado visible: una solicitud retirada se guarda como Cancelada, pero aquí se nombra por lo que pasó.</summary>
        public string StatusLabel => Withdrawn ? "Retirada por el docente" : PlanningStatus.Label(Status);
        public string StatusCss => Withdrawn ? "secondary" : Pages.LaboratoryOperations.PlanningStatusView.Css(Status);
        public int DaysLeft => (Date.Date - DateTime.Today).Days;
        public bool Urgent => DaysLeft <= 2;
        public bool Soon => !Urgent && DaysLeft <= 5;
        public bool Clean => Blocks == 0 && Competitors == 0 && MaterialShort == 0;
        public string TimeText => Start.HasValue ? $"{Start.Value:hh\\:mm}–{(End ?? Start.Value):hh\\:mm}" : "Sin horario";
    }

    public static readonly IReadOnlyList<(string Key, string Label, string Icon)> Tabs =
    [
        ("revisar", "Por revisar", "fas fa-inbox"),
        ("observadas", "Observadas", "fas fa-comment-dots"),
        ("aprobadas", "Aprobadas", "fas fa-check-circle"),
        ("rechazadas", "Rechazadas", "fas fa-times-circle"),
        ("retiradas", "Retiradas", "fas fa-undo"),
        ("todas", "Todas", "fas fa-list")
    ];

    public const int MaxRows = 150;

    public string Tab { get; private set; } = "revisar";
    public int ToReview => TabCounts.GetValueOrDefault("revisar");
    public int Observed => TabCounts.GetValueOrDefault("observadas");
    public IReadOnlyDictionary<string, int> TabCounts { get; private set; } = new Dictionary<string, int>();
    public List<InboxRow> Rows { get; private set; } = [];
    /// <summary>True si la consulta tenía más filas que el máximo mostrado.</summary>
    public bool Truncated { get; private set; }
    /// <summary>Control semanal (esta semana y las 3 siguientes): prácticas programadas frente a solicitudes recibidas.</summary>
    public IReadOnlyList<WeekCoverage> Coverage { get; private set; } = [];

    // Filtros (todos opcionales; se conservan en la URL al cambiar de pestaña).
    [BindProperty(SupportsGet = true)] public int? TeacherId { get; set; }
    [BindProperty(SupportsGet = true)] public int? SubjectId { get; set; }
    [BindProperty(SupportsGet = true)] public int? LaboratoryId { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }

    public bool HasFilters => TeacherId.HasValue || SubjectId.HasValue || LaboratoryId.HasValue || From.HasValue || To.HasValue
        || !string.IsNullOrWhiteSpace(Q);

    public IReadOnlyList<(int Id, string Name)> TeacherOptions { get; private set; } = [];
    public IReadOnlyList<(int Id, string Name)> SubjectOptions { get; private set; } = [];
    public IReadOnlyList<(int Id, string Name)> LaboratoryOptions { get; private set; } = [];

    /// <summary>Parámetros de ruta de los filtros vigentes (para los enlaces de pestañas).</summary>
    public Dictionary<string, string> FilterRoute(string tab)
    {
        var route = new Dictionary<string, string> { ["tab"] = tab };
        if (TeacherId.HasValue) route["teacherId"] = TeacherId.Value.ToString();
        if (SubjectId.HasValue) route["subjectId"] = SubjectId.Value.ToString();
        if (LaboratoryId.HasValue) route["laboratoryId"] = LaboratoryId.Value.ToString();
        if (From.HasValue) route["from"] = From.Value.ToString("yyyy-MM-dd");
        if (To.HasValue) route["to"] = To.Value.ToString("yyyy-MM-dd");
        if (!string.IsNullOrWhiteSpace(Q)) route["q"] = Q.Trim();
        return route;
    }

    /// <summary>Solicitudes de docentes: toda planificación con historial de revisión (nace con la acción "Enviada").</summary>
    private IQueryable<Planning> TeacherRequests() => _context.Plannings.AsNoTracking().Where(p => !p.IsDeleted && p.Reviews.Any());

    private static IQueryable<Planning> ForTab(IQueryable<Planning> query, string tab) => tab switch
    {
        "observadas" => query.Where(p => p.Status == PlanningStatus.Observed),
        "aprobadas" => query.Where(p => p.Status != PlanningStatus.Rejected && p.Reviews.Any(r =>
            r.Action == PlanningReviewAction.Approved || r.Action == PlanningReviewAction.ApprovedWithChanges)),
        "rechazadas" => query.Where(p => p.Status == PlanningStatus.Rejected),
        "retiradas" => query.Where(p => p.Reviews.Any(r => r.Action == PlanningReviewAction.Withdrawn)),
        "todas" => query,
        _ => query.Where(p => p.Status == PlanningStatus.Requested)
    };

    private IQueryable<Planning> ApplyFilters(IQueryable<Planning> query)
    {
        if (TeacherId is int teacherId) query = query.Where(p => p.TeacherPersonId == teacherId);
        if (SubjectId is int subjectId) query = query.Where(p => p.SubjectId == subjectId);
        if (LaboratoryId is int laboratoryId) query = query.Where(p => p.LaboratoryId == laboratoryId);
        if (From is DateTime from) query = query.Where(p => p.ScheduledDate >= from.Date);
        if (To is DateTime to)
        {
            var end = to.Date.AddDays(1);
            query = query.Where(p => p.ScheduledDate < end);
        }
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            // "PRA-00123" o "123" buscan por código; cualquier otro texto, por el nombre de la práctica.
            var digits = new string(term.Where(char.IsDigit).ToArray());
            var id = digits.Length is > 0 and <= 9 ? int.Parse(digits) : (int?)null;
            query = query.Where(p => p.PracticeName.Contains(term) || (p.Code != null && p.Code.Contains(term))
                || (id != null && p.Id == id));
        }
        return query;
    }

    public async Task OnGetAsync(string? tab)
    {
        Tab = Tabs.Any(t => t.Key == tab) ? tab! : "revisar";
        if (From.HasValue && To.HasValue && To < From) (From, To) = (To, From);

        // Los contadores respetan los filtros: dicen cuántas hay en cada pestaña para lo que se está buscando.
        var filtered = ApplyFilters(TeacherRequests());
        var counts = new Dictionary<string, int>();
        foreach (var t in Tabs)
            counts[t.Key] = await ForTab(filtered, t.Key).CountAsync();
        TabCounts = counts;
        if (Tab == "revisar")
            Coverage = await _requests.GetWeeklyCoverageAsync(DateTime.Today, 4);

        await LoadFilterOptionsAsync();

        var query = ForTab(filtered, Tab);
        var pendingTab = Tab is "revisar" or "observadas";
        var ordered = pendingTab
            ? query.OrderBy(p => p.ScheduledDate)
            : query.OrderByDescending(p => p.Reviews.Max(r => r.CreatedDate));
        var rows = await ordered
            .Take(MaxRows + 1)
            .Select(p => new
            {
                p.Id, p.Code, p.TeacherPersonId, p.PracticeName, Subject = p.Subject != null ? p.Subject.Name : null,
                Group = p.CourseGroup != null ? p.CourseGroup.Name : p.GroupName, p.ScheduledDate, p.StartTime, p.EndTime,
                Lab = p.Laboratory != null ? p.Laboratory.Code : null, p.LaboratoryId, p.CourseGroupId, p.StartPeriodId, p.PeriodCount,
                Items = p.Requirements.Count, p.Status, p.RequestDate,
                Comment = p.Reviews.OrderByDescending(r => r.CreatedDate).Select(r => r.Comment).FirstOrDefault(),
                LastAction = p.Reviews.OrderByDescending(r => r.CreatedDate).Select(r => (PlanningReviewAction?)r.Action).FirstOrDefault(),
                LastActionDate = p.Reviews.Max(r => (DateTime?)r.CreatedDate),
                Withdrawn = p.Reviews.Any(r => r.Action == PlanningReviewAction.Withdrawn)
            })
            .ToListAsync();
        Truncated = rows.Count > MaxRows;
        if (Truncated) rows.RemoveAt(rows.Count - 1);

        var teacherIds = rows.Select(r => r.TeacherPersonId).Distinct().ToList();
        var names = await _context.Interns.AsNoTracking().Where(i => teacherIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => string.IsNullOrWhiteSpace(i.Title) ? i.Name : i.Title + " " + i.Name);

        // Material de las pendientes: requerimientos en UNA consulta y evaluación por día (un solo rango/stock por fecha).
        var pendingRows = rows
            .Where(r => r.Status == PlanningStatus.Requested && r.LaboratoryId != null && r.StartPeriodId != null
                && r.PeriodCount != null && r.CourseGroupId != null)
            .ToList();
        var pendingIds = pendingRows.Select(r => r.Id).ToList();
        var linesByPlan = new Dictionary<int, IReadOnlyList<RequestLineInput>>();
        if (pendingIds.Count > 0)
        {
            var allLines = await _context.PlanningRequirements.AsNoTracking()
                .Where(l => pendingIds.Contains(l.PlanningId))
                .Select(l => new { l.PlanningId, l.ArticleId, Quantity = l.RequestedQuantity ?? l.ExpectedQuantity })
                .ToListAsync();
            foreach (var group in allLines.GroupBy(l => l.PlanningId))
                linesByPlan[group.Key] = group.Select(l => new RequestLineInput(l.ArticleId, l.Quantity)).ToList();
        }
        var materialByPlan = new Dictionary<int, IReadOnlyList<MaterialLineCheck>>();
        foreach (var dayGroup in pendingRows.GroupBy(r => r.ScheduledDate.Date))
        {
            var batch = dayGroup
                .Select(r => new MaterialDayRequest(r.Id, r.StartPeriodId!.Value, r.PeriodCount!.Value, linesByPlan.GetValueOrDefault(r.Id) ?? []))
                .ToList();
            foreach (var check in await _requests.CheckMaterialForDayAsync(dayGroup.Key, batch))
                materialByPlan[check.PlanningId] = check.Lines;
        }

        foreach (var r in rows)
        {
            int blocks = 0, competitors = 0, shortCount = 0, noStock = 0;
            // Solo las pendientes se analizan (las resueltas ya no compiten).
            if (r.Status == PlanningStatus.Requested && r.LaboratoryId is int labId && r.StartPeriodId is int startId && r.PeriodCount is int count && r.CourseGroupId is int groupId)
            {
                var check = await _requests.CheckSlotAsync(r.TeacherPersonId, new RequestInput
                {
                    CourseGroupId = groupId, Date = r.ScheduledDate, StartPeriodId = startId, PeriodCount = count, LaboratoryId = labId
                }, r.Id, forApproval: true);
                blocks = check.Blocks.Count();
                competitors = check.Issues.Count(i => i.Code == "PENDING_COMPETITION");
                if (materialByPlan.TryGetValue(r.Id, out var material))
                {
                    shortCount = material.Count(m => m.State == MaterialState.Short);
                    noStock = material.Count(m => m.State == MaterialState.NoStock);
                }
            }
            Rows.Add(new InboxRow(r.Id, Pages.LaboratoryOperations.L3Codes.Format(r.Id, r.Code), names.GetValueOrDefault(r.TeacherPersonId) ?? "—",
                r.PracticeName, r.Subject, r.Group, r.ScheduledDate, r.StartTime, r.EndTime, r.Lab, r.Items, r.Status, r.RequestDate,
                blocks, competitors, shortCount, noStock, r.Comment, r.LastActionDate, r.Withdrawn));
        }
    }

    /// <summary>Opciones de los filtros: solo docentes, materias y laboratorios que aparecen en alguna solicitud.</summary>
    private async Task LoadFilterOptionsAsync()
    {
        var keys = await TeacherRequests()
            .Select(p => new { p.TeacherPersonId, p.SubjectId, p.LaboratoryId })
            .Distinct().ToListAsync();
        var teacherIds = keys.Select(k => k.TeacherPersonId).Distinct().ToList();
        var subjectIds = keys.Where(k => k.SubjectId != null).Select(k => k.SubjectId!.Value).Distinct().ToList();
        var labIds = keys.Where(k => k.LaboratoryId != null).Select(k => k.LaboratoryId!.Value).Distinct().ToList();
        TeacherOptions = (await _context.Interns.AsNoTracking().Where(i => teacherIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name }).ToListAsync())
            .OrderBy(i => i.Name).Select(i => (i.Id, i.Name)).ToList();
        SubjectOptions = (await _context.Subjects.AsNoTracking().Where(x => subjectIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name }).ToListAsync())
            .OrderBy(x => x.Name).Select(x => (x.Id, x.Name)).ToList();
        LaboratoryOptions = (await _context.Laboratories.AsNoTracking().Where(l => labIds.Contains(l.Id))
            .Select(l => new { l.Id, l.Code, l.Name }).ToListAsync())
            .OrderBy(l => l.Code).Select(l => (l.Id, l.Code + " · " + l.Name)).ToList();
    }

    /// <summary>Aprueba en lote las solicitudes indicadas que siguen sin ningún aviso (se revalidan una por una).</summary>
    public async Task<IActionResult> OnPostApproveCleanAsync(int[] ids)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int ok = 0;
        var failed = new List<string>();
        foreach (var id in ids.Distinct())
        {
            // El lote nunca aprueba con faltantes: solo entra lo que el semáforo marcó como limpio.
            var result = await _requests.ApproveAsync(userId, id, "Aprobada en lote: sin choques ni faltantes.", null, allowShortage: false);
            if (result.Ok) ok++; else failed.Add(result.Message);
        }
        if (ok > 0) TempData.Success($"{ok} solicitud(es) aprobada(s). Los docentes ya fueron notificados.");
        if (failed.Count > 0) TempData.Warning($"{failed.Count} no se aprobaron: {string.Join(" ", failed.Take(3))}");
        return RedirectToPage();
    }
}
