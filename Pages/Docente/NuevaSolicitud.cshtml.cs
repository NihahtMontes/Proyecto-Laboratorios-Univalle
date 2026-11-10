using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.Docente;

/// <summary>
/// Asistente de solicitud de práctica en 4 pasos (qué → cuándo y dónde → material → enviar). Los pasos se
/// resuelven en el navegador con handlers JSON; el envío final revalida todo en el servidor (TeacherRequestService).
/// Con <c>id</c> corrige una solicitud observada; con <c>copyFrom</c> repite una anterior (sin fecha).
/// </summary>
public class NuevaSolicitudModel : TeacherPageBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IOccupancyService _occupancy;
    private readonly IArticleAvailabilityService _availability;

    public NuevaSolicitudModel(ApplicationDbContext db, ITeacherRequestService requests, IOccupancyService occupancy,
        IArticleAvailabilityService availability) : base(db, requests)
    {
        _occupancy = occupancy;
        _availability = availability;
    }

    public sealed record GroupOption(int Id, string Name, string Subject, string SubjectCode, int? Students, int? LabId, string? LabCode);
    public sealed record PeriodOption(int Id, int Order, string Start, string End);
    public sealed record LabOption(int Id, string Code, string Name, string? Type, int? Capacity);

    public List<GroupOption> Groups { get; private set; } = [];
    public List<PeriodOption> Periods { get; private set; } = [];
    public List<LabOption> Labs { get; private set; } = [];
    public List<(int Id, string Name)> Categories { get; private set; } = [];

    /// <summary>Estado inicial del asistente (corrección o repetición) serializado para el JavaScript.</summary>
    public string InitialStateJson { get; private set; } = "null";
    public int? EditingId { get; private set; }
    public string? EditingCode { get; private set; }
    public string? ObservationComment { get; private set; }

    [BindProperty] public RequestInput Input { get; set; } = new();
    [BindProperty] public string LinesJson { get; set; } = "[]";
    [BindProperty] public string? Note { get; set; }
    [BindProperty] public int? PlanningId { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id, int? copyFrom, int? scheduleId)
    {
        await LoadListsAsync();
        if (id == null && copyFrom == null && scheduleId is int programmed)
        {
            // Desde el cronograma: grupo, práctica y semana precargados; el docente elige día, horario y material.
            var practice = (await Requests.GetProgrammedAsync(Teacher.PersonId, Window, 200)).FirstOrDefault(p => p.ScheduleId == programmed);
            if (practice == null || Groups.All(g => g.Id != practice.CourseGroupId))
            {
                TempData.Warning("Esa práctica programada ya tiene solicitud o no es de tus grupos.");
                return RedirectToPage("./Index");
            }
            InitialStateJson = JsonSerializer.Serialize(new
            {
                groupId = practice.CourseGroupId,
                practiceId = practice.PracticeId,
                weekDate = practice.Date.ToString("yyyy-MM-dd"),
                periodCount = 2,
                labId = practice.LaboratoryId,
                observations = $"Práctica programada para el {practice.Date:dd/MM/yyyy}" + (practice.WeekNumber is int w ? $" (semana {w})." : "."),
                lines = Array.Empty<object>()
            }, Json);
            return Page();
        }
        var sourceId = id ?? copyFrom;
        if (sourceId is int source)
        {
            var plan = await Mine().Include(p => p.Requirements).Include(p => p.PracticeSchedule)
                .FirstOrDefaultAsync(p => p.Id == source);
            if (plan == null)
            {
                TempData.Error("La solicitud no existe.");
                return RedirectToPage("./Solicitudes");
            }
            if (id.HasValue && plan.Status != PlanningStatus.Observed)
            {
                TempData.Warning("Solo se corrige una solicitud observada.");
                return RedirectToPage("./Solicitud", new { id });
            }
            var lines = await Requests.GetLinesAsync(plan.Id);
            EditingId = id;
            EditingCode = plan.Code;
            if (id.HasValue)
                ObservationComment = await Db.PlanningReviews.AsNoTracking()
                    .Where(r => r.PlanningId == plan.Id && r.Action == PlanningReviewAction.Observed)
                    .OrderByDescending(r => r.CreatedDate).Select(r => r.Comment).FirstOrDefaultAsync();
            InitialStateJson = JsonSerializer.Serialize(new
            {
                planningId = id,
                groupId = Groups.Any(g => g.Id == plan.CourseGroupId) ? plan.CourseGroupId : null,
                practiceId = plan.PracticeSchedule?.PracticeId,
                students = plan.PracticeSchedule?.StudentCount,
                date = id.HasValue ? plan.ScheduledDate.ToString("yyyy-MM-dd") : null,
                startPeriodId = plan.StartPeriodId,
                periodCount = plan.PeriodCount ?? 2,
                labId = plan.LaboratoryId,
                observations = plan.Observations,
                lines = lines.Select(l => new { articleId = l.ArticleId, code = l.Code, name = l.Name, category = l.Category, unit = l.Unit, quantity = l.Quantity })
            }, Json);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            Input.Lines = JsonSerializer.Deserialize<List<RequestLineInput>>(LinesJson, Json) ?? [];
        }
        catch (JsonException)
        {
            Input.Lines = [];
        }
        var result = await Requests.SubmitAsync(Teacher, Input, PlanningId, Note);
        if (!result.Ok)
        {
            TempData.Error(result.Message);
            await LoadListsAsync();
            InitialStateJson = JsonSerializer.Serialize(new
            {
                planningId = PlanningId,
                groupId = Input.CourseGroupId,
                practiceId = Input.PracticeId,
                students = Input.StudentCount,
                date = Input.Date == default ? null : Input.Date.ToString("yyyy-MM-dd"),
                startPeriodId = Input.StartPeriodId,
                periodCount = Input.PeriodCount,
                labId = Input.LaboratoryId,
                observations = Input.Observations,
                lines = await DescribeLinesAsync(Input.Lines)
            }, Json);
            EditingId = PlanningId;
            return Page();
        }
        TempData.Success(result.Message);
        if (result.Warnings is { Count: > 0 }) TempData.Warning(string.Join(" ", result.Warnings));
        return RedirectToPage("./Solicitud", new { id = result.PlanningId, sent = true });
    }

    // ───────────── Handlers JSON del asistente ─────────────

    /// <summary>
    /// Prácticas del catálogo de la materia del grupo, con su kit y cuántas veces el grupo ya la realizó, la tiene
    /// aprobada o en revisión en la gestión (solo informativo: una práctica se puede repetir las veces que se necesite).
    /// </summary>
    public async Task<IActionResult> OnGetPracticesAsync(int groupId)
    {
        var group = await MyGroupAsync(groupId);
        if (group == null) return new JsonResult(Array.Empty<object>());
        var practices = await Db.Practices.AsNoTracking()
            .Where(p => p.SubjectId == group.SubjectId && p.Status == GeneralStatus.Activo)
            .Select(p => new
            {
                p.Id, p.Code, p.Number, p.Name, p.Description,
                KitCount = Db.PracticeRequirements.Count(r => r.PracticeId == p.Id && !r.IsRemoved)
            })
            .ToListAsync();
        // Sesiones del grupo por práctica y estado (las rechazadas y retiradas no cuentan).
        var sessions = await Db.Plannings.AsNoTracking()
            .Where(pl => !pl.IsDeleted && pl.CourseGroupId == group.Id && pl.PracticeSchedule != null
                && pl.Status != PlanningStatus.Rejected && pl.Status != PlanningStatus.Cancelled)
            .GroupBy(pl => new { pl.PracticeSchedule!.PracticeId, pl.Status })
            .Select(g => new { g.Key.PracticeId, g.Key.Status, Count = g.Count() })
            .ToListAsync();
        int Count(int practiceId, params string[] statuses) =>
            sessions.Where(x => x.PracticeId == practiceId && statuses.Contains(x.Status)).Sum(x => x.Count);

        var ordered = practices
            .OrderBy(p => int.TryParse(p.Number, out var n) ? n : int.MaxValue)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                p.Id, p.Code, p.Number, p.Name, p.Description, p.KitCount,
                done = Count(p.Id, PlanningStatus.Finalized),
                planned = Count(p.Id, PlanningStatus.Approved, PlanningStatus.Planned),
                pending = Count(p.Id, PlanningStatus.Requested, PlanningStatus.Observed)
            });
        return new JsonResult(ordered);
    }

    /// <summary>Kit de la práctica escalado a la cantidad de estudiantes.</summary>
    public async Task<IActionResult> OnGetKitAsync(int practiceId, int groupId, int? students)
    {
        var group = await MyGroupAsync(groupId);
        if (group == null) return new JsonResult(Array.Empty<object>());
        var kit = await Db.PracticeRequirements.AsNoTracking()
            .Where(r => r.PracticeId == practiceId && !r.IsRemoved && r.Article!.Status == GeneralStatus.Activo
                && r.Practice!.SubjectId == group.SubjectId)
            .Select(r => new
            {
                r.ArticleId, r.Article!.Code, r.Article.Name, r.Quantity, r.IsScalable,
                Category = r.Article.ArticleCategory != null ? r.Article.ArticleCategory.Name : r.Article.Category,
                Unit = r.Article.UnitOfMeasure
            })
            .ToListAsync();
        var studentCount = students ?? group.StudentCount;
        var lines = kit.GroupBy(k => k.ArticleId).Select(g =>
        {
            var first = g.First();
            decimal total = 0;
            string? note = null;
            foreach (var line in g)
            {
                var (qty, n) = RequirementPickerService.Scale(line.Quantity, line.IsScalable, studentCount, group.ScalingFactor);
                total += qty ?? 1;
                note ??= n;
            }
            return new { articleId = first.ArticleId, code = first.Code, name = first.Name, category = first.Category, unit = first.Unit, quantity = total, note };
        }).OrderBy(l => l.name);
        return new JsonResult(lines);
    }

    /// <summary>Semana de un ambiente para elegir horario: ocupado / separación / pendiente / mío / libre, sin datos de otros docentes.</summary>
    public async Task<IActionResult> OnGetWeekAsync(int labId, DateTime date, int? excludeId)
    {
        var monday = date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        var saturday = monday.AddDays(5);
        var exclude = excludeId is int id ? new[] { id } : null;
        var blocks = await _occupancy.GetAsync(new OccupancyQuery(monday, saturday, [labId], null, exclude));
        var mineBlocks = await _occupancy.GetAsync(new OccupancyQuery(monday, saturday, null, [Teacher.PersonId], exclude));
        var pending = await Db.Plannings.AsNoTracking()
            .Where(p => !p.IsDeleted && PlanningStatus.PendingReview.Contains(p.Status) && p.LaboratoryId == labId
                && p.ScheduledDate >= monday && p.ScheduledDate < saturday.AddDays(1) && p.StartPeriod != null && p.PeriodCount != null
                && (excludeId == null || p.Id != excludeId))
            .Select(p => new { Date = p.ScheduledDate.Date, Start = p.StartPeriod!.Order, Count = p.PeriodCount!.Value, Mine = p.TeacherPersonId == Teacher.PersonId })
            .ToListAsync();
        var managementId = Window.ManagementId ?? 0;
        var days = await Db.AcademicDays.AsNoTracking()
            .Where(d => d.AcademicWeek.ManagementId == managementId && d.Date >= monday && d.Date <= saturday)
            .Select(d => new { d.Date, d.Status, d.Reason })
            .ToListAsync();
        var periods = await Db.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();

        var result = new List<object>();
        for (var d = monday; d <= saturday; d = d.AddDays(1))
        {
            var day = days.FirstOrDefault(x => x.Date == d);
            var enabled = day != null && day.Status == AcademicDayStatus.Enabled && Window.Contains(d);
            var offReason = day == null ? "Sin clases" : day.Status != AcademicDayStatus.Enabled ? (day.Reason ?? "Suspendido")
                : !Window.Contains(d) ? "Fuera de plazo" : null;
            var cells = periods.Select(p =>
            {
                string state;
                if (!enabled) state = "off";
                else if (mineBlocks.Any(b => b.Date == d && p.Order >= b.StartOrder && p.Order <= b.LastOrder)) state = "mine";
                else if (blocks.Any(b => b.Date == d && p.Order >= b.StartOrder && p.Order <= b.LastOrder)) state = "busy";
                else if (blocks.Any(b => b.Date == d && (p.Order == b.StartOrder - 1 || p.Order == b.LastOrder + 1))) state = "gap";
                else if (pending.Any(x => x.Date == d && p.Order >= x.Start && p.Order < x.Start + x.Count)) state = "pending";
                else state = "free";
                return new { periodId = p.Id, order = p.Order, state };
            }).ToList();
            result.Add(new { date = d.ToString("yyyy-MM-dd"), enabled, offReason, cells });
        }
        return new JsonResult(new
        {
            monday = monday.ToString("yyyy-MM-dd"),
            periods = periods.Select(p => new { p.Id, p.Order, start = p.StartTime.ToString(@"hh\:mm"), end = p.EndTime.ToString(@"hh\:mm") }),
            days = result
        });
    }

    public sealed class SuggestRequest
    {
        public int GroupId { get; set; }
        public int PeriodCount { get; set; } = 2;
        public int? LabId { get; set; }
        public List<RequestLineInput> Lines { get; set; } = [];
    }

    public async Task<IActionResult> OnPostSuggestAsync([FromBody] SuggestRequest request)
    {
        var suggestions = await Requests.SuggestAsync(Teacher.PersonId, request.GroupId, Math.Clamp(request.PeriodCount, 1, 12), request.LabId, request.Lines);
        var es = new System.Globalization.CultureInfo("es-BO");
        return new JsonResult(suggestions.Select(s => new
        {
            date = s.Date.ToString("yyyy-MM-dd"),
            dateText = s.Date.ToString("dddd d 'de' MMMM", es),
            s.StartPeriodId, s.StartOrder, s.PeriodCount, s.LaboratoryId, s.LaboratoryCode, s.LaboratoryName,
            time = $"{s.Start:hh\\:mm}–{s.End:hh\\:mm}",
            periodsText = PlanningSlot.PeriodsText(s.StartOrder, s.PeriodCount),
            s.MaterialIssues, s.Reasons
        }));
    }

    public sealed class CheckRequest
    {
        public int? PlanningId { get; set; }
        public int GroupId { get; set; }
        public DateTime Date { get; set; }
        public int StartPeriodId { get; set; }
        public int PeriodCount { get; set; }
        public int LabId { get; set; }
        public int? Students { get; set; }
        public List<RequestLineInput> Lines { get; set; } = [];
    }

    /// <summary>Validación en vivo del horario y del material (no guarda nada).</summary>
    public async Task<IActionResult> OnPostCheckAsync([FromBody] CheckRequest request)
    {
        if (await MyGroupAsync(request.GroupId) == null)
            return new JsonResult(new { issues = new[] { new { code = "GROUP", severity = "Block", title = "Grupo", message = "Selecciona uno de tus grupos." } }, material = Array.Empty<object>() });
        var input = new RequestInput
        {
            CourseGroupId = request.GroupId, Date = request.Date, StartPeriodId = request.StartPeriodId,
            PeriodCount = request.PeriodCount, LaboratoryId = request.LabId, StudentCount = request.Students
        };
        var slot = await Requests.CheckSlotAsync(Teacher.PersonId, input, request.PlanningId);
        var material = await Requests.CheckMaterialAsync(request.Date, request.StartPeriodId, request.PeriodCount, request.Lines, request.PlanningId);
        return new JsonResult(new
        {
            issues = slot.Issues.Select(i => new { i.Code, severity = i.Severity.ToString(), i.Title, i.Message }),
            material = material.Select(m => new
            {
                m.ArticleId, m.Code, m.Name, m.Requested, m.Stock, m.UsedByOthers, m.Free,
                state = m.State.ToString(), m.StateText, m.StateCss, m.Explanation,
                // Solo el horario de uso: el docente no ve datos de prácticas de otros docentes.
                uses = m.Uses.Select(u => new { u.Code, u.TimeText, u.Quantity })
            })
        });
    }

    /// <summary>Buscador de artículos activos (nombre, código o categoría) con su stock total.</summary>
    public async Task<IActionResult> OnGetArticlesAsync(string? term, int? categoryId)
    {
        var query = Db.Articles.AsNoTracking().Where(a => a.Status == GeneralStatus.Activo);
        if (categoryId is int category) query = query.Where(a => a.ArticleCategoryId == category);
        foreach (var word in (term ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(4))
            query = query.Where(a => a.Name.Contains(word) || a.Code.Contains(word));
        var rows = await query.OrderBy(a => a.Name).Take(30)
            .Select(a => new
            {
                articleId = a.Id, code = a.Code, name = a.Name, unit = a.UnitOfMeasure,
                category = a.ArticleCategory != null ? a.ArticleCategory.Name : a.Category
            })
            .ToListAsync();
        var stock = await _availability.GetStockAsync(rows.Select(r => r.articleId), null);
        return new JsonResult(rows.Select(r => new { r.articleId, r.code, r.name, r.unit, r.category, stock = stock.GetValueOrDefault(r.articleId)?.Available }));
    }

    // ───────────── Apoyo ─────────────

    private async Task<CourseGroup?> MyGroupAsync(int groupId) =>
        await Db.CourseGroups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId && g.TeacherPersonId == Teacher.PersonId && g.Status == GeneralStatus.Activo
                && g.ManagementId == (Window.ManagementId ?? 0));

    private async Task LoadListsAsync()
    {
        var managementId = Window.ManagementId ?? 0;
        Groups = await Db.CourseGroups.AsNoTracking()
            .Where(g => g.TeacherPersonId == Teacher.PersonId && g.ManagementId == managementId && g.Status == GeneralStatus.Activo)
            .OrderBy(g => g.Subject!.Name).ThenBy(g => g.Name)
            .Select(g => new GroupOption(g.Id, g.Name, g.Subject!.Name, g.Subject.Code, g.StudentCount, g.LaboratoryId,
                g.Laboratory != null ? g.Laboratory.Code : null))
            .ToListAsync();
        Periods = (await Db.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync())
            .Select(p => new PeriodOption(p.Id, p.Order, p.StartTime.ToString(@"hh\:mm"), p.EndTime.ToString(@"hh\:mm"))).ToList();
        Labs = await Db.Laboratories.AsNoTracking()
            .Where(l => l.Status == GeneralStatus.Activo && (l.Type == null || !TeacherRequestService.NonRequestableTypes.Contains(l.Type)))
            .OrderBy(l => l.Code)
            .Select(l => new LabOption(l.Id, l.Code, l.Name, l.Type, l.Capacity))
            .ToListAsync();
        Categories = (await Db.ArticleCategories.AsNoTracking().Where(c => c.Status == GeneralStatus.Activo).OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name }).ToListAsync()).Select(c => (c.Id, c.Name)).ToList();
    }

    private async Task<object> DescribeLinesAsync(IReadOnlyList<RequestLineInput> lines)
    {
        var ids = lines.Select(l => l.ArticleId).ToList();
        var articles = await Db.Articles.AsNoTracking().Where(a => ids.Contains(a.Id))
            .Select(a => new { a.Id, a.Code, a.Name, a.UnitOfMeasure, Category = a.ArticleCategory != null ? a.ArticleCategory.Name : a.Category })
            .ToDictionaryAsync(a => a.Id);
        return lines.Where(l => articles.ContainsKey(l.ArticleId)).Select(l => new
        {
            articleId = l.ArticleId, code = articles[l.ArticleId].Code, name = articles[l.ArticleId].Name,
            category = articles[l.ArticleId].Category, unit = articles[l.ArticleId].UnitOfMeasure, quantity = l.Quantity
        }).ToList();
    }
}
