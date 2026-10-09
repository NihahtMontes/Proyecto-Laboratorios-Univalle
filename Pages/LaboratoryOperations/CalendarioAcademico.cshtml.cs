using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Calendario académico de una gestión: semanas de lunes a sábado generadas desde las fechas de la gestión,
/// con el estado de cada día (habilitado/suspendido) y la carga de planificaciones por día.
/// Consultar es para toda la gestión; generar semanas y suspender/habilitar días es exclusivo de
/// Administrador/SuperAdmin sobre una gestión editable (Activa o Inactiva).
/// </summary>
[Authorize(Roles = AuthorizationHelper.ManagementRoles)]
public class CalendarioAcademicoModel : PageModel
{
    private const int Weekdays = 6;

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IAcademicCalendarService _calendar;

    public CalendarioAcademicoModel(ApplicationDbContext context, IAcademicManagementService managements,
        IAcademicCalendarService calendar)
    {
        _context = context;
        _managements = managements;
        _calendar = calendar;
    }

    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    public AcademicManagementSummary? Management { get; private set; }
    public SelectList ManagementList { get; private set; } = new(Enumerable.Empty<SelectListItem>());

    /// <summary>True para Administrador/SuperAdmin: los únicos que pueden escribir en el calendario.</summary>
    public bool CanWrite { get; private set; }

    public bool CanEditCalendar => CanWrite && Management is { IsEditable: true };

    public IReadOnlyList<WeekRow> Weeks { get; private set; } = [];

    public int EnabledDays => Weeks.SelectMany(w => w.Days).Count(d => d.Status == AcademicDayStatus.Enabled);
    public int SuspendedDays => Weeks.SelectMany(w => w.Days).Count(d => d.Status == AcademicDayStatus.Suspended);

    /// <summary>Día de una semana del calendario. Status null = el día no existe (queda fuera de la gestión).</summary>
    public sealed record DayCell(int DayId, DateTime Date, AcademicDayStatus? Status, string? Reason, int PlanCount)
    {
        public bool Exists => Status.HasValue;

        public string DateText => Date.ToString("dd/MM");
    }

    public sealed record WeekRow(int Number, DateTime Monday, DateTime Saturday, IReadOnlyList<DayCell> Days);

    public async Task<IActionResult> OnGetAsync()
    {
        CanWrite = User.IsInRole(AuthorizationHelper.RoleAdministrator) || User.IsInRole(AuthorizationHelper.RoleSuperAdmin);

        Management = await _managements.ResolveAsync(ManagementId);
        ManagementId = Management?.Id;
        ManagementList = await BuildManagementListAsync();
        if (Management == null)
            return Page();

        await LoadCalendarAsync();
        return Page();
    }

    /// <summary>Genera o actualiza las semanas de la gestión (confirma con SweetAlert en la vista).</summary>
    public async Task<IActionResult> OnPostGenerateAsync(GenerateInput input)
    {
        if (!User.IsInRole(AuthorizationHelper.RoleAdministrator) && !User.IsInRole(AuthorizationHelper.RoleSuperAdmin))
        {
            TempData.Error("Solo un administrador puede generar o actualizar el calendario académico.");
            return RedirectToPage(new { managementId = input.ManagementId });
        }

        var result = await _calendar.GenerateWeeksAsync(input.ManagementId);
        if (result.Ok) TempData.Success(result.Message);
        else TempData.Error(result.Message);
        return RedirectToPage(new { managementId = input.ManagementId });
    }

    /// <summary>Suspende (status = 1, motivo obligatorio) o habilita (status = 0) un día del calendario.</summary>
    public async Task<IActionResult> OnPostSetDayAsync(DayStatusInput input)
    {
        if (!User.IsInRole(AuthorizationHelper.RoleAdministrator) && !User.IsInRole(AuthorizationHelper.RoleSuperAdmin))
        {
            TempData.Error("Solo un administrador puede suspender o habilitar días del calendario académico.");
            return RedirectToPage(new { managementId = input.ManagementId });
        }

        var result = await _calendar.SetDayStatusAsync(input.DayId, input.Status, input.Reason);
        if (result.Ok) TempData.Success(result.Message);
        else TempData.Error(result.Message);
        return RedirectToPage(new { managementId = input.ManagementId });
    }

    private async Task<SelectList> BuildManagementListAsync()
    {
        var managements = await _managements.ListAsync();
        var items = managements.Select(m => new SelectListItem
        {
            Value = m.Id.ToString(),
            Text = $"{m.Code} · {m.StatusText}{(m.IsCurrent ? " (actual)" : "")}"
        });
        return new SelectList(items, "Value", "Text", ManagementId);
    }

    private async Task LoadCalendarAsync()
    {
        var weeks = await _context.AcademicWeeks.AsNoTracking()
            .Include(w => w.Days)
            .Where(w => w.ManagementId == Management!.Id)
            .OrderBy(w => w.Number)
            .ToListAsync();
        if (weeks.Count == 0)
        {
            Weeks = [];
            return;
        }

        // Planificaciones vigentes (no borradas y con estado que ocupa) por fecha dentro del calendario.
        var from = weeks[0].StartDate.Date;
        var to = weeks[^1].EndDate.Date;
        var dates = await _context.Plannings.AsNoTracking()
            .Where(p => p.ManagementId == Management!.Id
                && !PlanningStatus.NonOccupying.Contains(p.Status)
                && p.ScheduledDate >= from && p.ScheduledDate < to.AddDays(1))
            .Select(p => p.ScheduledDate)
            .ToListAsync();
        var planCounts = dates.GroupBy(d => d.Date).ToDictionary(g => g.Key, g => g.Count());

        Weeks = weeks.Select(w =>
        {
            var days = Enumerable.Range(0, Weekdays).Select(i =>
            {
                var date = w.StartDate.Date.AddDays(i);
                var day = w.Days.FirstOrDefault(d => d.Date == date);
                return new DayCell(day?.Id ?? 0, date, day?.Status, day?.Reason, planCounts.GetValueOrDefault(date));
            }).ToList();
            return new WeekRow(w.Number, w.StartDate.Date, w.EndDate.Date, days);
        }).ToList();
    }

    /// <summary>Entrada del botón "Generar / actualizar semanas".</summary>
    public sealed class GenerateInput
    {
        public int ManagementId { get; set; }
    }

    /// <summary>Entrada de suspender/habilitar un día (status: 0 habilitado, 1 suspendido).</summary>
    public sealed class DayStatusInput
    {
        public int ManagementId { get; set; }

        public int DayId { get; set; }

        public AcademicDayStatus Status { get; set; }

        public string? Reason { get; set; }
    }
}
