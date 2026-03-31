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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "El responsable/solicitante es obligatorio")]
            [Display(Name = "Responsable / Solicitante")]
            public int BorrowerId { get; set; }

            [Required(ErrorMessage = "El tipo de salida es obligatorio")]
            [Display(Name = "Tipo de Salida")]
            public DepartureType Type { get; set; } = DepartureType.ExternalMaintenance;

            [Required(ErrorMessage = "La fecha de salida es obligatoria")]
            [Display(Name = "Fecha de Salida")]
            [DataType(DataType.Date)]
            public DateTime DepartureDate { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "La fecha estimada de devolución es obligatoria")]
            [Display(Name = "Fecha Estimada de Devolución")]
            [DataType(DataType.Date)]
            public DateTime EstimatedReturnDate { get; set; } = DateTime.Today.AddDays(30);

            [StringLength(500)]
            [Display(Name = "Observaciones")]
            public string? DepartureObservations { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false, int? managementPlanId = null)
        {
            await LoadLists();

            if (equipmentUnitId.HasValue)
            {
                Input.EquipmentUnitId = equipmentUnitId.Value;
            }

            ManagementPlanId = managementPlanId;
            ViewData["IsWizard"] = isWizard;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            if (!ModelState.IsValid)
            {
                await LoadLists();
                return Page();
            }

            try
            {
                var departure = new Departure
                {
                    EquipmentUnitId = Input.EquipmentUnitId,
                    BorrowerId = Input.BorrowerId,
                    Type = Input.Type,
                    DepartureDate = Input.DepartureDate,
                    EstimatedReturnDate = Input.EstimatedReturnDate,
                    DepartureObservations = Input.DepartureObservations?.Trim(),
                    Status = LoanStatus.Active,
                    CreatedDate = DateTime.UtcNow
                };

                var currentUser = await _userManager.GetUserAsync(User);
                departure.CreatedById = int.TryParse(currentUser?.Id.ToString(), out var uid) ? uid : (int?)null;

                _context.Departures.Add(departure);
                await _context.SaveChangesAsync();

                // Update equipment status
                var equipmentUnit = await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);
                if (equipmentUnit != null)
                {
                    equipmentUnit.CurrentStatus = EquipmentStatus.OnLoan;
                    _context.EquipmentUnits.Update(equipmentUnit);
                    await _context.SaveChangesAsync();
                }

                // Wizard trigger: advance to Kardex
                if (ManagementPlanId.HasValue)
                {
                    var plan = await _context.ManagementPlans.FindAsync(ManagementPlanId.Value);
                    if (plan != null)
                    {
                        plan.DepartureId = departure.Id;
                        plan.CurrentPhase = WizardPhase.Kardex;
                        plan.CurrentState = WizardEquipmentState.AwaitingKardex;
                        await _context.SaveChangesAsync();

                        if (isWizard)
                        {
                            return RedirectToPage("/Wizard/Index", new { Step = 5 });
                        }
                    }
                }

                TempData["Success"] = "Salida de equipo registrada exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al guardar: {ex.Message}";
                await LoadLists();
                return Page();
            }
        }

        private async Task LoadLists()
        {
            ViewData["EquipmentUnitId"] = new SelectList(
                await _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Where(u => u.CurrentStatus != EquipmentStatus.Deleted)
                    .Select(u => new { u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" })
                    .ToListAsync(),
                "Id", "Name");

            ViewData["BorrowerId"] = new SelectList(
                await _context.People
                    .Where(p => p.Status == GeneralStatus.Activo)
                    .Select(p => new { p.Id, p.FullName })
                    .ToListAsync(),
                "Id", "FullName");
        }
    }
}
