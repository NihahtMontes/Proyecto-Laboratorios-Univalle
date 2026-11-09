using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.Docente;

/// <summary>Detalle de una solicitud/práctica del docente: estado, seguimiento, material (pedido vs aprobado) y acciones.</summary>
public class SolicitudModel : TeacherPageBase
{
    public SolicitudModel(ApplicationDbContext db, ITeacherRequestService requests) : base(db, requests) { }

    public TeacherPlanningRow Row { get; private set; } = null!;
    public int? Students { get; private set; }
    public string? Observations { get; private set; }
    public IReadOnlyList<RequestLineView> Lines { get; private set; } = [];
    public IReadOnlyList<ReviewEventView> History { get; private set; } = [];
    public bool JustSent { get; private set; }
    public bool HasDeparture { get; private set; }
    public int IncidentCount { get; private set; }

    public bool CanEdit => Row.Status == PlanningStatus.Observed && Window.IsOpen;
    public bool CanWithdraw => PlanningStatus.PendingReview.Contains(Row.Status);
    public bool CanPrint => Row.Status is PlanningStatus.Approved or PlanningStatus.Finalized;
    public string Code => L3Codes.Format(Row.Id, Row.Code);

    public async Task<IActionResult> OnGetAsync(int id, bool sent = false)
    {
        if (!await LoadAsync(id))
        {
            TempData.Error("La solicitud no existe.");
            return RedirectToPage("./Solicitudes");
        }
        JustSent = sent;
        return Page();
    }

    public async Task<IActionResult> OnPostWithdrawAsync(int id, string? reason)
    {
        var result = await Requests.WithdrawAsync(Teacher, id, reason);
        if (result.Ok) TempData.Success(result.Message); else TempData.Error(result.Message);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnGetFichaAsync(int id)
    {
        if (!await LoadAsync(id) || !CanPrint)
        {
            TempData.Warning("La ficha está disponible cuando la práctica está aprobada.");
            return RedirectToPage(new { id });
        }
        var approval = History.LastOrDefault(h => h.Action is PlanningReviewAction.Approved or PlanningReviewAction.ApprovedWithChanges);
        var pdf = PracticeSheetPdf.Generate(new PracticeSheet(
            Code, TeacherStatus.Text(Row.Status), Row.Practice, Row.Subject ?? "—", Row.Group ?? "—", Teacher.Name, Students,
            Row.Date, Row.TimeText, Row.PeriodsText, $"{Row.LabCode} · {Row.LabName}", Observations, Lines,
            approval?.UserName, approval == null ? null : BoliviaTime.ToLocal(approval.Date)));
        return File(pdf, "application/pdf", $"Ficha_{Code}.pdf");
    }

    private async Task<bool> LoadAsync(int id)
    {
        var row = await Project(Mine().Where(p => p.Id == id)).FirstOrDefaultAsync();
        if (row == null) return false;
        Row = row;
        var extra = await Mine().Where(p => p.Id == id)
            .Select(p => new
            {
                Students = p.PracticeSchedule != null ? p.PracticeSchedule.StudentCount : null,
                p.Observations,
                HasDeparture = p.Departures.Any(),
                Incidents = p.Incidents.Count
            })
            .FirstAsync();
        Students = extra.Students;
        Observations = extra.Observations;
        HasDeparture = extra.HasDeparture;
        IncidentCount = extra.Incidents;
        Lines = await Requests.GetLinesAsync(id);
        History = await Requests.GetHistoryAsync(id);
        return true;
    }
}
