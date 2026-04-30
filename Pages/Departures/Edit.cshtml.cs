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

namespace Proyecto_Laboratorios_Univalle.Pages.Departures
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "El responsable es obligatorio")]
            [Display(Name = "Responsable / Solicitante")]
            public int BorrowerId { get; set; }

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

            [Required(ErrorMessage = "La fecha estimada de devolución es obligatoria")]
            [Display(Name = "Fecha Estimada de Devolución")]
            [DataType(DataType.Date)]
            public DateTime EstimatedReturnDate { get; set; }

            [Display(Name = "Fecha Real de Devolución")]
            [DataType(DataType.DateTime)]
            public DateTime? ActualReturnDate { get; set; }

            [StringLength(500)]
            [Display(Name = "Observaciones de Salida")]
            public string? DepartureObservations { get; set; }

            [StringLength(500)]
            [Display(Name = "Observaciones de Devolución")]
            public string? ReturnObservations { get; set; }
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
                .FirstOrDefaultAsync(d => d.Id == id);

            if (departure == null) return NotFound();

            Input = new InputModel
            {
                Id = departure.Id,
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

            CargarListas();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                CargarListas();
                return Page();
            }

            var departureDB = await _context.Departures
                .Include(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .FirstOrDefaultAsync(d => d.Id == Input.Id);

            if (departureDB == null) return NotFound();

            departureDB.BorrowerId = Input.BorrowerId;
            departureDB.Type = Input.Type;
            departureDB.Status = Input.Status;
            departureDB.DepartureDate = Input.DepartureDate;
            departureDB.EstimatedReturnDate = Input.EstimatedReturnDate;
            departureDB.ActualReturnDate = Input.ActualReturnDate;
            departureDB.DepartureObservations = Input.DepartureObservations?.Trim();
            departureDB.ReturnObservations = Input.ReturnObservations?.Trim();

            // Si se devolvió, actualizar el estado del equipo
            if (Input.Status == LoanStatus.Returned && Input.ActualReturnDate.HasValue)
            {
                var unit = await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);
                if (unit != null)
                {
                    unit.CurrentStatus = EquipmentStatus.Operational;
                    _context.EquipmentUnits.Update(unit);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Salida #{departureDB.Id} actualizada correctamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al guardar: {ex.Message}";
                CargarListas();
                return Page();
            }
        }

        private void CargarListas()
        {
            var equipos = _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(u => u.Equipment!.Name)
                .AsEnumerable()
                .Select(u => new
                {
                    Id = u.Id,
                    DisplayName = $"{u.Equipment!.Name} (Inv: {u.InventoryNumber})"
                })
                .ToList();

            ViewData["EquipmentUnitId"] = new SelectList(equipos, "Id", "DisplayName", Input.EquipmentUnitId);

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
