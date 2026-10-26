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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Pages.Departures
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<EditModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física")]
            public int? EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "El responsable es obligatorio")]
            [Display(Name = "Responsable / Solicitante")]
            public int? BorrowerId { get; set; }

            [Required(ErrorMessage = "El tipo de salida es obligatorio")]
            [Display(Name = "Tipo de Salida")]
            public DepartureType Type { get; set; }

            [Required(ErrorMessage = "El estado es obligatorio")]
            [Display(Name = "Estado")]
            public LoanStatus Status { get; set; }

            [Required(ErrorMessage = "La fecha de salida es obligatoria")]
            [Display(Name = "Fecha de Salida")]
            [DataType(DataType.Date)]
            public DateTime DepartureDate { get; set; }

            [Display(Name = "Fecha Estimada de Devolución")]
            [DataType(DataType.Date)]
            public DateTime? EstimatedReturnDate { get; set; }

            [Display(Name = "Fecha Real de Devolución")]
            [DataType(DataType.DateTime)]
            public DateTime? ActualReturnDate { get; set; }

            [StringLength(500, ErrorMessage = "Las observaciones no pueden superar 500 caracteres")]
            [Display(Name = "Observaciones de Salida")]
            public string? DepartureObservations { get; set; }

            [StringLength(500, ErrorMessage = "Las observaciones no pueden superar 500 caracteres")]
            [Display(Name = "Observaciones de Devolución")]
            public string? ReturnObservations { get; set; }

            public List<ItemInput> Items { get; set; } = new();

            public class ItemInput
            {
                public int? Id { get; set; }
                public int? EquipmentUnitId { get; set; }
                public string? ProductName { get; set; }
                [Range(0.01, 999999, ErrorMessage = "La cantidad debe ser mayor a 0")]
                public decimal? Quantity { get; set; }
                public string UnitOfMeasure { get; set; } = "UNIDAD";
                public string? Observations { get; set; }
            }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var departure = await _context.Departures
                .Include(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu!.Laboratory)
                .Include(d => d.Borrower)
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (departure == null) return NotFound();

            // Salida de práctica académica (L-3): su entrega, devolución y cierre se gestionan
            // en el circuito L-3 para mantener la coherencia del stock y de las devoluciones.
            if (departure.PlanningId.HasValue)
            {
                TempData.Info($"La salida {departure.Code ?? $"SAL-{departure.Id:00000}"} es de una práctica académica: su entrega, devolución y cierre se gestionan en «Entrega y devolución de material».");
                return RedirectToPage("/LaboratoryOperations/L3", new { id = departure.PlanningId });
            }

            int facultyId = departure.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
            int labId = departure.EquipmentUnit?.LaboratoryId ?? 0;

            Input = new InputModel
            {
                Id = departure.Id,
                FacultyId = facultyId,
                LaboratoryId = labId,
                EquipmentUnitId = departure.EquipmentUnitId,
                BorrowerId = departure.BorrowerId,
                Type = departure.Type,
                Status = departure.Status,
                DepartureDate = departure.DepartureDate,
                EstimatedReturnDate = departure.EstimatedReturnDate,
                ActualReturnDate = departure.ActualReturnDate,
                DepartureObservations = departure.DepartureObservations,
                ReturnObservations = departure.ReturnObservations
            };

            foreach (var item in departure.Items)
            {
                Input.Items.Add(new InputModel.ItemInput
                {
                    Id = item.Id,
                    EquipmentUnitId = item.EquipmentUnitId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitOfMeasure = item.UnitOfMeasure ?? "UNIDAD",
                    Observations = item.Observations
                });
            }

            // Detect Wizard Plan
            var plan = await _context.ManagementPlans.FirstOrDefaultAsync(p => p.DepartureId == id);
            if (plan != null)
            {
                ViewData["IsWizard"] = true;
                ViewData["ManagementPlanId"] = plan.Id;
                ViewData["CurrentPhaseInt"] = (int)plan.CurrentPhase;
                if (plan.MaintenanceId.HasValue) ViewData["LinkedMaintenanceId"] = plan.MaintenanceId.Value;
            }

            CargarListas(facultyId, labId, departure.EquipmentUnitId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // ── (a) La unidad física NO se toma del POST ──
            // La salida conserva la unidad con la que fue registrada: se anula el campo
            // postado para ignorar cualquier cambio manual y reconstruir la verdad desde BD.
            var departureUnit = await _context.Departures
                .AsNoTracking()
                .Where(d => d.Id == Input.Id)
                .Select(d => new { d.EquipmentUnitId, d.PlanningId, d.Code })
                .FirstOrDefaultAsync();

            if (departureUnit == null) return NotFound();

            // Salida de práctica académica (L-3): bloquear la edición manual ANTES de validar o
            // guardar, ya que marcar "Devuelto" aquí no registra la devolución ni repone stock.
            if (departureUnit.PlanningId.HasValue)
            {
                TempData.Info($"La salida {departureUnit.Code ?? $"SAL-{Input.Id:00000}"} es de una práctica académica: su entrega, devolución y cierre se gestionan en «Entrega y devolución de material».");
                return RedirectToPage("/LaboratoryOperations/L3", new { id = departureUnit.PlanningId });
            }

            ModelState.Remove("Input.EquipmentUnitId");
            Input.EquipmentUnitId = departureUnit.EquipmentUnitId;

            // ── (b)/(c) Coherencia de devolución y fechas ──
            // Errores a nivel de modelo (la vista muestra el resumen ModelOnly).
            if (Input.Status == LoanStatus.Returned && !Input.ActualReturnDate.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Indique la fecha real de devolución para marcar la salida como devuelta.");
            }

            if (Input.ActualReturnDate.HasValue && Input.ActualReturnDate.Value.Date < Input.DepartureDate.Date)
            {
                ModelState.AddModelError(string.Empty, "La fecha real de devolución no puede ser anterior a la fecha de salida.");
            }

            if (Input.EstimatedReturnDate.HasValue && Input.EstimatedReturnDate.Value.Date < Input.DepartureDate.Date)
            {
                ModelState.AddModelError(string.Empty, "La fecha estimada de devolución no puede ser anterior a la fecha de salida.");
            }

            if (!ModelState.IsValid)
            {
                var departureForError = await _context.Departures.Include(m => m.EquipmentUnit).ThenInclude(eu => eu!.Laboratory).Include(d => d.Items).FirstOrDefaultAsync(m => m.Id == Input.Id);
                int facultyId = departureForError?.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
                int labId = departureForError?.EquipmentUnit?.LaboratoryId ?? 0;
                CargarListas(facultyId, labId, Input.EquipmentUnitId);

                var planForError = await _context.ManagementPlans.FirstOrDefaultAsync(p => p.DepartureId == Input.Id);
                if (planForError != null)
                {
                    ViewData["IsWizard"] = true;
                    ViewData["ManagementPlanId"] = planForError.Id;
                    ViewData["CurrentPhaseInt"] = (int)planForError.CurrentPhase;
                    if (planForError.MaintenanceId.HasValue) ViewData["LinkedMaintenanceId"] = planForError.MaintenanceId.Value;
                }

                return Page();
            }

            var departureDB = await _context.Departures
                .Include(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(d => d.Items)
                .AsTracking()
                .FirstOrDefaultAsync(d => d.Id == Input.Id);

            if (departureDB == null) return NotFound();

            // Estado previo de la salida (antes de aplicar los cambios del POST).
            var statusBefore = departureDB.Status;

            // Una salida ya devuelta no se reabre: dejaría la unidad sin coherencia con su historial.
            if (statusBefore == LoanStatus.Returned && Input.Status != LoanStatus.Returned)
            {
                TempData.Warning($"La salida #{departureDB.Id} ya fue devuelta y no se puede reabrir. Registre una salida nueva si el equipo vuelve a salir.");
                return RedirectToPage("./Index");
            }

            departureDB.BorrowerId = Input.BorrowerId;
            departureDB.Type = Input.Type;
            departureDB.Status = Input.Status;
            departureDB.DepartureDate = Input.DepartureDate;
            departureDB.EstimatedReturnDate = Input.EstimatedReturnDate;
            departureDB.ActualReturnDate = Input.ActualReturnDate;
            departureDB.DepartureObservations = Input.DepartureObservations?.Trim();
            departureDB.ReturnObservations = Input.ReturnObservations?.Trim();

            var existingItems = departureDB.Items.ToList();
            var inputItemIds = Input.Items?
                .Where(i => i.Id.GetValueOrDefault() > 0)
                .Select(i => i.Id.GetValueOrDefault())
                .ToList() ?? new List<int>();

            foreach (var existing in existingItems)
            {
                if (!inputItemIds.Contains(existing.Id))
                {
                    existing.IsRemoved = true;
                }
            }

            if (Input.Items != null)
            {
                foreach (var itemInput in Input.Items.Where(i => i.Id.GetValueOrDefault() > 0 || !string.IsNullOrWhiteSpace(i.ProductName)))
                {
                    if (itemInput.Id.HasValue && itemInput.Id > 0)
                    {
                        var existingItem = existingItems.FirstOrDefault(e => e.Id == itemInput.Id);
                        if (existingItem != null)
                        {
                            existingItem.ProductName = string.IsNullOrWhiteSpace(itemInput.ProductName)
                                ? null
                                : itemInput.ProductName.Trim();
                            existingItem.Quantity = itemInput.Quantity;
                            existingItem.UnitOfMeasure = itemInput.UnitOfMeasure?.Trim() ?? "UNIDAD";
                            existingItem.Observations = itemInput.Observations?.Trim();
                        }
                    }
                    else
                    {
                        _context.DepartureItems.Add(new DepartureItem
                        {
                            DepartureId = departureDB.Id,
                            EquipmentUnitId = itemInput.EquipmentUnitId ?? Input.EquipmentUnitId,
                            ProductName = itemInput.ProductName!.Trim(),
                            Quantity = itemInput.Quantity,
                            UnitOfMeasure = itemInput.UnitOfMeasure?.Trim() ?? "UNIDAD",
                            Observations = itemInput.Observations?.Trim()
                        });
                    }
                }
            }

            // ── (d) Devolución: la unidad vuelve a Operativo SOLO si estaba En Préstamo ──
            // Se usa la unidad tracked de la propia salida (nunca el POST) y se registra el historial.
            if (Input.Status == LoanStatus.Returned && Input.ActualReturnDate.HasValue
                && departureDB.EquipmentUnit is EquipmentUnit returnedUnit
                && returnedUnit.CurrentStatus == EquipmentStatus.OnLoan)
            {
                returnedUnit.CurrentStatus = EquipmentStatus.Operational;
                await RegisterUnitStatusChangeAsync(
                    returnedUnit.Id,
                    EquipmentStatus.Operational,
                    $"Devolución de la salida #{departureDB.Id}");
            }

            // ── (f) Cancelación: ninguna salida Cancelada puede dejar la unidad En Préstamo ──
            if (Input.Status == LoanStatus.Cancelled
                && departureDB.EquipmentUnit is EquipmentUnit cancelledUnit
                && cancelledUnit.CurrentStatus == EquipmentStatus.OnLoan)
            {
                var hasOtherActiveDeparture = await _context.Departures
                    .AnyAsync(d => d.EquipmentUnitId == cancelledUnit.Id
                                    && d.Id != departureDB.Id
                                    && (d.Status == LoanStatus.Active || d.Status == LoanStatus.Overdue));

                if (!hasOtherActiveDeparture)
                {
                    cancelledUnit.CurrentStatus = EquipmentStatus.Operational;
                    await RegisterUnitStatusChangeAsync(
                        cancelledUnit.Id,
                        EquipmentStatus.Operational,
                        $"Cancelación de la salida #{departureDB.Id}");
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Salida #{departureDB.Id} actualizada correctamente.");
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo actualizar la salida {DepartureId}.", Input.Id);
                TempData.Error("No se pudo guardar la salida. Intente nuevamente.");
                var departureForError = await _context.Departures.Include(m => m.EquipmentUnit).ThenInclude(eu => eu!.Laboratory).FirstOrDefaultAsync(m => m.Id == Input.Id);
                int facultyId = departureForError?.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
                int labId = departureForError?.EquipmentUnit?.LaboratoryId ?? 0;
                CargarListas(facultyId, labId, Input.EquipmentUnitId);
                return Page();
            }
        }

        /// <summary>Longitud máxima de Reason en EquipmentStateHistory (StringLength 500 en BD).</summary>
        private const int HistoryReasonMaxLength = 500;

        /// <summary>
        /// Cierra el historial abierto de la unidad (EndDate = max(ahora, StartDate), como
        /// EquipmentUnits/Edit) y abre uno nuevo con el estado y motivo indicados.
        /// </summary>
        private async Task RegisterUnitStatusChangeAsync(int unitId, EquipmentStatus newStatus, string reason)
        {
            var now = DateTime.UtcNow;

            var lastHistory = await _context.EquipmentStateHistories
                .Where(h => h.EquipmentUnitId == unitId && h.EndDate == null)
                .OrderByDescending(h => h.StartDate)
                .AsTracking()
                .FirstOrDefaultAsync();

            if (lastHistory != null)
            {
                lastHistory.EndDate = now > lastHistory.StartDate ? now : lastHistory.StartDate;
                _context.EquipmentStateHistories.Update(lastHistory);
            }

            _context.EquipmentStateHistories.Add(new EquipmentStateHistory
            {
                EquipmentUnitId = unitId,
                Status = newStatus,
                StartDate = now,
                Reason = reason.Length <= HistoryReasonMaxLength ? reason : reason[..HistoryReasonMaxLength]
            });
        }

        private void CargarListas(int facultyId = 0, int laboratoryId = 0, int? equipmentUnitId = null)
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties.Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name), "Id", "Name", facultyId);
            
            if (facultyId > 0)
                ViewData["LaboratoryId"] = new SelectList(_context.Laboratories.Where(l => l.FacultyId == facultyId).OrderBy(l => l.Name), "Id", "Name", laboratoryId);
            else
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            var equipos = _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(u => u.Equipment!.Name)
                .AsEnumerable()
                .Select(u => new
                {
                    Id = u.Id,
                    DisplayName = $"{u.Equipment!.Name} (Inv: {u.InventoryNumber})"
                })
                .ToList();

            ViewData["EquipmentUnitId"] = new SelectList(equipos, "Id", "DisplayName", equipmentUnitId);

            var people = _context.People
                .Where(p => p.Status == GeneralStatus.Activo)
                .AsEnumerable()
                .Select(p => new { Id = p.Id, Name = p.FullName })
                .OrderBy(x => x.Name)
                .ToList();

            ViewData["BorrowerId"] = new SelectList(people, "Id", "Name", Input.BorrowerId);
        }
    }
}
