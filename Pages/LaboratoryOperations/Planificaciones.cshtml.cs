using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>Historial de planificaciones académicas: activar (cierra la activa actual) o cerrar.</summary>
public class PlanificacionesModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planning;
    private readonly ILogger<PlanificacionesModel> _logger;

    public PlanificacionesModel(ApplicationDbContext context, IPlanningService planning, ILogger<PlanificacionesModel> logger)
    {
        _context = context;
        _planning = planning;
        _logger = logger;
    }

    public List<PlanningRow> Plannings { get; private set; } = [];
    public PlanningRow? Active => Plannings.FirstOrDefault(p => p.Status == AcademicPlanningStatus.Active);

    public sealed record PlanningRow(int Id, string Code, string Name, string ManagementCode, string FacultyName, string CareerName,
        DateTime StartDate, DateTime EndDate, AcademicPlanningStatus Status, int WeekCount, int PracticeCount)
    {
        public string StatusText => Status switch
        {
            AcademicPlanningStatus.Active => "Activa",
            AcademicPlanningStatus.Completed => "Cerrada",
            _ => "Inactiva"
        };
        public string StatusCss => Status switch
        {
            AcademicPlanningStatus.Active => "success",
            AcademicPlanningStatus.Completed => "secondary",
            _ => "warning"
        };
    }

    public async Task OnGetAsync()
    {
        Plannings = await _context.AcademicPlannings.AsNoTracking()
            .OrderBy(p => p.Status == AcademicPlanningStatus.Active ? 0 : 1)
            .ThenByDescending(p => p.StartDate)
            .Select(p => new PlanningRow(
                p.Id,
                p.Management.Code + " · " + (p.Career.Code ?? ("PLAN-" + p.Id)),
                p.Name, p.Management.Code, p.Faculty.Name, p.Career.Name, p.StartDate, p.EndDate, p.Status,
                p.Weeks.Count(),
                p.Practices.Count(r => r.Status != L3RequestStatus.Cancelled)))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostActivateAsync(int id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var planning = await _context.AcademicPlannings.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (planning == null)
            {
                TempData.Error("La planificación no existe.");
                return RedirectToPage();
            }
            if (planning.Status == AcademicPlanningStatus.Active)
            {
                TempData.Info("La planificación ya está activa.");
                return RedirectToPage("./Index");
            }

            var closed = await _planning.ActivateAsync(planning);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _planning.InvalidateCache();

            TempData.Success(closed > 0
                ? $"Planificación «{planning.Name}» activada. Se cerró la planificación que estaba activa."
                : $"Planificación «{planning.Name}» activada.");
            return RedirectToPage("./Index");
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            // Otra activación concurrente ganó el índice único de "una sola activa".
            await transaction.RollbackAsync();
            _planning.InvalidateCache();
            TempData.Warning("Otra planificación se activó al mismo tiempo. Revise cuál quedó activa e intente nuevamente.");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo activar la planificación {PlanningId}", id);
            TempData.Error("No se pudo activar la planificación. Intente nuevamente.");
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        var planning = await _context.AcademicPlannings.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (planning == null || planning.Status != AcademicPlanningStatus.Active)
        {
            TempData.Error("Solo se puede cerrar la planificación activa.");
            return RedirectToPage();
        }

        _planning.Close(planning);
        await _context.SaveChangesAsync();
        _planning.InvalidateCache();
        TempData.Success($"Planificación «{planning.Name}» cerrada. Queda disponible en modo consulta.");
        return RedirectToPage();
    }
}
