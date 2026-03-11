using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Loans
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            LoadLists();
            return Page();
        }

        [BindProperty]
        public LoanInputModel Input { get; set; } = new();

        public class LoanInputModel
        {
            [Required(ErrorMessage = "La facultad es obligatoria")]
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "Debe seleccionar un activo")]
            [Display(Name = "Activo / Utensilio")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "Debe seleccionar un solicitante")]
            [Display(Name = "Solicitante")]
            public int BorrowerId { get; set; }

            [Required(ErrorMessage = "La fecha de préstamo es obligatoria")]
            [DataType(DataType.DateTime)]
            [Display(Name = "Fecha de Préstamo")]
            public DateTime LoanDate { get; set; } = DateTime.UtcNow;

            [Required(ErrorMessage = "La fecha estimada de devolución es obligatoria")]
            [DataType(DataType.Date)]
            [Display(Name = "Devolución Estimada")]
            public DateTime EstimatedReturnDate { get; set; } = DateTime.UtcNow.AddDays(1);

            [Display(Name = "Observaciones de Salida")]
            public string? DepartureObservations { get; set; }
        }

        // AJAX Handlers
        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .OrderBy(x => x.name)
                .ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<JsonResult> OnGetUnitsByLabAsync(int laboratoryId)
        {
            var units = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus == EquipmentStatus.Operational)
                .OrderBy(u => u.Equipment!.Name)
                .ThenBy(u => u.InventoryNumber)
                .Select(u => new { 
                    id = u.Id, 
                    eqName = u.Equipment != null ? u.Equipment.Name : "Equipo",
                    inv = u.InventoryNumber,
                    sn = u.SerialNumber
                })
                .ToListAsync();

            var result = units.Select(x => new {
                id = x.id,
                name = $"{x.inv} - {x.eqName} (S/N: {x.sn ?? "N/A"})"
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadLists();
                return Page();
            }

            var unit = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .FirstOrDefaultAsync(u => u.Id == Input.EquipmentUnitId);

            if (unit == null)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "El activo seleccionado no existe.");
                LoadLists();
                return Page();
            }

            if (unit.CurrentStatus != EquipmentStatus.Operational)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", $"El activo '{unit.Equipment!.Name}' no está operativo (Estado: {unit.CurrentStatus}).");
                LoadLists();
                return Page();
            }

            var loan = new Loan
            {
                EquipmentUnitId = Input.EquipmentUnitId,
                BorrowerId = Input.BorrowerId,
                LoanDate = Input.LoanDate,
                EstimatedReturnDate = Input.EstimatedReturnDate,
                DepartureObservations = Input.DepartureObservations,
                Status = LoanStatus.Active
            };

            // Cerramos el estado actual (si existe) y abrimos uno nuevo en el histórico "OnLoan"
            var lastHistory = await _context.EquipmentStateHistories
                .Where(h => h.EquipmentUnitId == unit.Id && h.EndDate == null)
                .OrderByDescending(h => h.StartDate)
                .FirstOrDefaultAsync();
            
            if (lastHistory != null)
            {
                lastHistory.EndDate = DateTime.UtcNow;
                _context.EquipmentStateHistories.Update(lastHistory);
            }

            var newHistory = new EquipmentStateHistory
            {
                EquipmentUnitId = unit.Id,
                Status = EquipmentStatus.OnLoan,
                StartDate = DateTime.UtcNow,
                Reason = $"Préstamo asignado al solicitante (ID: {loan.BorrowerId}). " + Input.DepartureObservations
            };
            _context.EquipmentStateHistories.Add(newHistory);

            unit.CurrentStatus = EquipmentStatus.OnLoan;
            _context.Loans.Add(loan);
            _context.EquipmentUnits.Update(unit);

            await _context.SaveChangesAsync();
            TempData.Success(NotificationHelper.Loans.Created(unit.Equipment!.Name));

            return RedirectToPage("./Index");
        }

        private void LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name), "Id", "Name");
            
            ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            var people = _context.People
                .Where(p => p.Status == GeneralStatus.Activo)
                .Select(p => new { Id = p.Id, Text = $"{p.FullName} (ID: {p.Id})" })
                .ToList();
            ViewData["BorrowerId"] = new SelectList(people, "Id", "Text");
        }
    }
}
