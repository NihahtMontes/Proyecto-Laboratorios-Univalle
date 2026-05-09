using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Departures
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class MassCreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementContext;

        public MassCreateModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IManagementContextService managementContext)
        {
            _context = context;
            _userManager = userManager;
            _managementContext = managementContext;
        }

        [BindProperty(SupportsGet = true)]
        public bool IsWizard { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? LabId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        // Fechas globales para toda la sesión masiva
        [BindProperty]
        [Required(ErrorMessage = "La fecha de salida es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime DepartureDate { get; set; } = DateTime.Today;

        [BindProperty]
        [Required(ErrorMessage = "La fecha de retorno es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime EstimatedReturnDate { get; set; } = DateTime.Today.AddDays(15);

        [BindProperty]
        public List<RowInput> Rows { get; set; } = new();

        public SelectList LabList { get; set; } = default!;
        public string? LabName { get; set; }

        public class RowInput
        {
            public int PlanId { get; set; }
            public int EquipmentUnitId { get; set; }
            // Nombre visible del equipo; en wizard el servidor deriva el valor definitivo.
            public string ProductName { get; set; } = string.Empty;
            // true = este equipo se incluye en la salida masiva
            public bool Include { get; set; } = true;
        }

        public async Task OnGetAsync()
        {
            var labs = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Name)
                .ToListAsync();
            LabList = new SelectList(labs, "Id", "Name", LabId);

            if (LabId.HasValue)
            {
                var lab = await _context.Laboratories.FindAsync(LabId.Value);
                LabName = lab?.Name;
            }

            ViewData["IsWizard"] = IsWizard;
        }

        public async Task<JsonResult> OnGetEquipmentByLab(int labId, int? managementId = null)
        {
            Management? activeMgmt = null;

            if (managementId.HasValue)
                activeMgmt = await _context.Managements.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == managementId.Value && m.Status == ManagementStatus.Active);

            activeMgmt ??= await _managementContext.GetCurrentManagementAsync();

            if (activeMgmt == null)
                return new JsonResult(Array.Empty<object>());

            var plans = await _context.ManagementPlans
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Include(p => p.Maintenance)
                .Where(p => p.ManagementId == activeMgmt.Id
                         && p.CurrentPhase == WizardPhase.Exit
                         && p.CurrentState == WizardEquipmentState.AwaitingDeparture
                         && (p.EquipmentUnit!.LaboratoryId == labId || p.EquipmentUnit!.LaboratoryId == null))
                .AsNoTracking()
                .OrderBy(p => p.EquipmentUnit!.InventoryNumber)
                .ToListAsync();

            var result = plans.Select(p => new
            {
                planId = p.Id,
                equipmentUnitId = p.EquipmentUnitId,
                maintenanceId = p.MaintenanceId,
                technicianId = p.Maintenance?.TechnicianId,
                name = p.EquipmentUnit?.Equipment?.Name ?? "Sin nombre",
                inventoryNumber = p.EquipmentUnit?.InventoryNumber ?? "N/A",
                brand = p.EquipmentUnit?.Equipment?.Brand ?? "",
                hasTechnician = p.Maintenance?.TechnicianId != null,
                hasLaboratory = p.EquipmentUnit?.LaboratoryId != null
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validar fechas
            if (DepartureDate == default || EstimatedReturnDate == default)
            {
                TempData.Error("Las fechas de salida y retorno son obligatorias.");
                await LoadLabList();
                return Page();
            }

            if (EstimatedReturnDate < DepartureDate)
            {
                TempData.Error("La fecha de retorno debe ser igual o posterior a la fecha de salida.");
                await LoadLabList();
                return Page();
            }

            var includedRows = Rows?.Where(r => r.Include).ToList();
            if (includedRows == null || includedRows.Count == 0)
            {
                var receivedRows = Rows == null || Rows.Count == 0
                    ? "sin filas recibidas"
                    : string.Join("; ", Rows.Select((r, i) => $"#{i}: Include={r.Include}, PlanId={r.PlanId}, EquipmentUnitId={r.EquipmentUnitId}"));

                TempData.Error($"Debe incluir al menos un equipo para registrar la salida. Datos recibidos: {receivedRows}");
                await LoadLabList();
                return Page();
            }

            // Resolver gestión
            Management? activeMgmt = null;
            if (ManagementId.HasValue)
                activeMgmt = await _context.Managements.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == ManagementId.Value);
            activeMgmt ??= await _managementContext.GetCurrentManagementAsync();

            if (activeMgmt == null)
            {
                TempData.Error("No hay gestión activa.");
                await LoadLabList();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            int created = 0;
            int skipped = 0;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in includedRows)
                {
                    var plan = await _context.ManagementPlans
                        .AsTracking()
                        .Include(p => p.Maintenance)
                        .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                        .FirstOrDefaultAsync(p => p.Id == row.PlanId);

                    if (plan == null) continue;

                    // Técnico SIEMPRE debe venir de L-8; laboratorio debe estar asignado
                    if (plan.Maintenance?.TechnicianId == null || !plan.EquipmentUnitId.HasValue || plan.EquipmentUnit?.LaboratoryId == null)
                    {
                        skipped++;
                        continue;
                    }

                    var technicianId = plan.Maintenance.TechnicianId.Value;
                    var equipmentUnitId = plan.EquipmentUnitId.Value;
                    var productName = $"{plan.EquipmentUnit?.Equipment?.Name ?? "Equipo"} ({plan.EquipmentUnit?.InventoryNumber ?? "S/N"})";
                    var borrower = await _context.People.FindAsync(technicianId);
                    var inferredType = (borrower is Extern || borrower?.Category == PersonCategory.Externo)
                        ? DepartureType.ExternalMaintenance
                        : DepartureType.InternalMaintenance;

                    var departure = new Departure
                    {
                        EquipmentUnitId = equipmentUnitId,
                        BorrowerId = technicianId,
                        Type = inferredType,
                        DepartureDate = DepartureDate,
                        EstimatedReturnDate = EstimatedReturnDate,
                        DepartureObservations = null,
                        Status = LoanStatus.Active,
                        CreatedDate = DateTime.UtcNow,
                        ManagementId = activeMgmt.Id,
                        CreatedById = user?.Id
                    };

                    _context.Departures.Add(departure);

                    // Ítem de salida derivado desde el equipo real, no desde el HTML.
                    _context.DepartureItems.Add(new DepartureItem
                    {
                        Departure = departure,
                        EquipmentUnitId = equipmentUnitId,
                        ProductName = productName,
                        Quantity = 1,
                        UnitOfMeasure = "UNIDAD",
                        Observations = null
                    });

                    // Actualizar estado del equipo
                    var unit = await _context.EquipmentUnits.FindAsync(equipmentUnitId);
                    if (unit != null)
                        unit.CurrentStatus = EquipmentStatus.OnLoan;

                    // Avanzar el plan a Kardex
                    plan.Departure = departure;
                    plan.CurrentPhase = WizardPhase.Kardex;
                    plan.CurrentState = WizardEquipmentState.AwaitingKardex;

                    await _context.SaveChangesAsync();
                    created++;
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                var detail = ex.InnerException?.Message ?? ex.Message;
                TempData.Error($"Error al registrar salidas: {detail}");
                await LoadLabList();
                return Page();
            }

            if (created == 0)
            {
                var attemptedRows = string.Join("; ", includedRows.Select((r, i) => $"#{i}: PlanId={r.PlanId}, EquipmentUnitId={r.EquipmentUnitId}"));
                TempData.Error($"No se registró ninguna salida. Omitidos: {skipped}. Filas procesadas: {attemptedRows}. Verifique que los equipos seleccionados tengan técnico L-8 y plan válido.");
                await LoadLabList();
                return Page();
            }

            var msg = $"{created} salida(s) registrada(s) correctamente.";
            if (skipped > 0) msg += $" {skipped} equipo(s) omitido(s) por no tener técnico en L-8.";
            TempData.Success(msg);

            if (IsWizard)
                return RedirectToPage("/Index", new { ShowWizard = true, Step = 5, SelectedLabId = LabId, ManagementId = activeMgmt.Id });

            return RedirectToPage("./Index");
        }

        private async Task LoadLabList()
        {
            var labs = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Name)
                .ToListAsync();
            LabList = new SelectList(labs, "Id", "Name", LabId);
            ViewData["IsWizard"] = IsWizard;
        }
    }
}
