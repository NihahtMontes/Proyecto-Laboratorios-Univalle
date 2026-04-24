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
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Departures
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementService;

        public CreateModel(ApplicationDbContext context, UserManager<User> userManager, IManagementContextService managementService)
        {
            _context = context;
            _userManager = userManager;
            _managementService = managementService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "La facultad es obligatoria")]
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "El responsable/solicitante es obligatorio")]
            [Display(Name = "Responsable / Solicitante / Proveedor")]
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
            public DateTime EstimatedReturnDate { get; set; } = DateTime.Today.AddDays(15);

            [StringLength(500)]
            [Display(Name = "Observaciones")]
            public string? DepartureObservations { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false, int? managementPlanId = null)
        {
            ManagementPlanId = managementPlanId;
            ViewData["IsWizard"] = isWizard;

            if (equipmentUnitId.HasValue)
            {
                var unit = await _context.EquipmentUnits
                    .Include(u => u.Laboratory)
                    .FirstOrDefaultAsync(u => u.Id == equipmentUnitId.Value);

                if (unit != null)
                {
                    Input.EquipmentUnitId = unit.Id;
                    Input.LaboratoryId = unit.LaboratoryId ?? 0;
                    Input.FacultyId = unit.Laboratory?.FacultyId ?? 0;
                }
            }

            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.Maintenance)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);

                if (plan?.Maintenance?.TechnicianId != null)
                {
                    Input.BorrowerId = plan.Maintenance.TechnicianId.Value;
                    ViewData["IsLockedBorrower"] = true;
                }
            }

            await LoadLists();
            return Page();
        }

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
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(u => u.Equipment!.Name)
                .ThenBy(u => u.InventoryNumber)
                .Select(u => new {
                    id = u.Id,
                    eqName = u.Equipment != null ? u.Equipment.Name : "Equipo",
                    inv = u.InventoryNumber
                })
                .ToListAsync();

            var result = units.Select(x => new {
                id = x.id,
                name = $"{x.eqName} (Inv: {x.inv})"
            });

            return new JsonResult(result);
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
                // Inferir DepartureType desde Person
                var borrower = await _context.People.FindAsync(Input.BorrowerId);
                var inferredType = (borrower is Extern || borrower?.Category == PersonCategory.Externo) 
                    ? DepartureType.ExternalMaintenance 
                    : DepartureType.InternalLoan;

                var departure = new Departure
                {
                    EquipmentUnitId = Input.EquipmentUnitId,
                    BorrowerId = Input.BorrowerId,
                    Type = inferredType,
                    DepartureDate = Input.DepartureDate,
                    EstimatedReturnDate = Input.EstimatedReturnDate,
                    DepartureObservations = Input.DepartureObservations?.Trim(),
                    Status = LoanStatus.Active,
                    CreatedDate = DateTime.UtcNow,
                    ManagementId = (await _managementService.GetCurrentManagementAsync()).Id
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
                            return RedirectToPage("/Index", new { ShowWizard = true, Step = 5, SelectedLabId = Input.LaboratoryId });
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
            ViewData["FacultyId"] = new SelectList(await _context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name).ToListAsync(), "Id", "Name", Input.FacultyId);

            if (Input.FacultyId > 0)
                ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == Input.FacultyId).ToListAsync(), "Id", "Name", Input.LaboratoryId);
            else
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            if (Input.LaboratoryId > 0)
                ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == Input.LaboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", Input.EquipmentUnitId);
            else
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            // Fix: FullName is NotMapped and TPT inheritance causes translation issues.
            // We fetch the data and then evaluate the FullName in memory.
            var people = await _context.People
                .Where(p => p.Status == GeneralStatus.Activo)
                .ToListAsync();

            var borrowerList = people
                .Select(p => new { 
                    Id = p.Id, 
                    Name = p is Intern i ? i.Name : (p is Extern e ? e.Name : "Persona #" + p.Id) 
                })
                .OrderBy(x => x.Name)
                .ToList();

            ViewData["BorrowerId"] = new SelectList(borrowerList, "Id", "Name", Input.BorrowerId);
        }
    }
}
