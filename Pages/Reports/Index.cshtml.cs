using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Reports
{
    /// <summary>
    /// DTO que describe qué formularios puede generar un equipo
    /// basado en la fase actual del ManagementPlan del wizard.
    /// Regla: Si el plan está en fase N, significa que las fases 1..N ya se completaron.
    /// </summary>
    public class EquipmentUnitReportStatus
    {
        public EquipmentUnit Unit { get; set; } = null!;
        public bool HasL6 { get; set; }
        public bool HasL7 { get; set; }
        public bool HasL8 { get; set; }
        public bool HasL3 { get; set; }
        public bool HasAdquisicion { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            ICurrentUserService currentUser,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        public SelectList LaboratoriesList { get; set; } = null!;
        public List<EquipmentUnitReportStatus> EquipmentStatuses { get; set; } = new();
        public Laboratory? SelectedLaboratory { get; set; }
        public string CurrentUserFullName { get; set; } = "";
        public string PrintDate { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        public async Task OnGetAsync()
        {
            try
            {
                var labs = await _context.Laboratories
                    .OrderBy(l => l.Name)
                    .ToListAsync();
                LaboratoriesList = new SelectList(labs, "Id", "Name");

                // Obtener nombre del usuario actual
                if (_currentUser.UserId.HasValue)
                {
                    var user = await _context.Users.FindAsync(_currentUser.UserId.Value);
                    CurrentUserFullName = user != null
                        ? $"{user.FirstName} {user.LastName}".Trim()
                        : "Sistema";
                }

                if (!SelectedLabId.HasValue) return;

                SelectedLaboratory = await _context.Laboratories
                    .FirstOrDefaultAsync(l => l.Id == SelectedLabId);

                var units = await _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Where(u => u.LaboratoryId == SelectedLabId)
                    .OrderBy(u => u.InventoryNumber)
                    .ToListAsync();

                if (!units.Any()) return;

                var unitIds = units.Select(u => u.Id).ToList();

                // ============================================================
                // CONSULTA SEGURA: Evitamos GroupBy con Global Query Filters
                // (el query filter de ManagementPlan hace INNER JOIN a Managements
                //  lo cual rompe el GroupBy en EF Core 9 y causa crash 0xffffffff)
                //
                // Estrategia: traer solo los campos planos necesarios (sin Include)
                // y agrupar en memoria.
                // ============================================================
                var planPhases = await _context.ManagementPlans
                    .Where(p => p.EquipmentUnitId != null
                             && unitIds.Contains(p.EquipmentUnitId.Value)
                             && (!ManagementId.HasValue || p.ManagementId == ManagementId.Value))
                    .Select(p => new
                    {
                        UnitId = p.EquipmentUnitId!.Value,
                        Phase = (int)p.CurrentPhase
                    })
                    .ToListAsync();

                // Agrupar en memoria (C# side) para evitar el crash de EF
                var phasesPerUnit = planPhases
                    .GroupBy(p => p.UnitId)
                    .ToDictionary(g => g.Key, g => g.Max(p => p.Phase));

                foreach (var unit in units)
                {
                    phasesPerUnit.TryGetValue(unit.Id, out var maxPhase);

                    // La lógica de fases del Wizard:
                    // CurrentPhase = N → la fase N está EN PROGRESO, las fases 1..(N-1) están COMPLETADAS
                    // Ejemplo: CurrentPhase=4 (L3 Salida) → L6, L7, L8 completados, L3 pendiente
                    // Por eso usamos ">" (estrictamente mayor): el reporte está disponible
                    // solo cuando su fase ya fue SUPERADA (la siguiente fase ya comenzó)
                    EquipmentStatuses.Add(new EquipmentUnitReportStatus
                    {
                        Unit = unit,
                        HasL6 = maxPhase > (int)WizardPhase.Verification,       // disponible desde fase 2+
                        HasL7 = maxPhase > (int)WizardPhase.TechnicalRequest,   // disponible desde fase 3+
                        HasL8 = maxPhase > (int)WizardPhase.Maintenance,        // disponible desde fase 4+
                        HasL3 = maxPhase > (int)WizardPhase.Exit,               // disponible desde fase 5+
                        HasAdquisicion = maxPhase >= (int)WizardPhase.Disbursement, // disponible en fase 6 (terminal)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando el Centro de Reportes para el Lab {LabId}", SelectedLabId);
                // Garantizar que la página siempre renderice aunque falle la consulta de fases
                LaboratoriesList ??= new SelectList(new List<Laboratory>(), "Id", "Name");
            }
        }
    }
}
