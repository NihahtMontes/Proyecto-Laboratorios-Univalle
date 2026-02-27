using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            LoadLists();
            
            Input = new InputModel
            {
                ScheduledDate = DateTime.Now,
                CostDetails = new List<CostDetail>()
            };

            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

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

            [Required(ErrorMessage = "El tipo de mantenimiento es obligatorio")]
            [Display(Name = "Tipo de Mantenimiento")]
            public int MaintenanceTypeId { get; set; }

            [Required(ErrorMessage = "El técnico es obligatorio")]
            [Display(Name = "Técnico Responsable")]
            public int TechnicianId { get; set; }

            [Display(Name = "Solicitud Relacionada")]
            public int? RequestId { get; set; }

            [Required(ErrorMessage = "La descripción es obligatoria")]
            [Display(Name = "Descripción del Trabajo")]
            public string Description { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Fecha Programada")]
            [DataType(DataType.Date)]
            public DateTime ScheduledDate { get; set; } = DateTime.Now;

            [Display(Name = "Fecha Inicio Real")]
            [DataType(DataType.DateTime)]
            public DateTime? StartDate { get; set; }

            [Display(Name = "Fecha Fin Real")]
            [DataType(DataType.DateTime)]
            public DateTime? EndDate { get; set; }

            [Display(Name = "Costo Real Total")]
            public decimal ActualCost { get; set; }

            [Display(Name = "Observaciones")]
            public string? Observations { get; set; }

            [Display(Name = "Recomendaciones")]
            public string? Recommendations { get; set; }

            [Required]
            [Display(Name = "Estado Inicial")]
            public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

            public List<CostDetail> CostDetails { get; set; } = new();
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

        public async Task<IActionResult> OnPostAsync()
        {
            // technical validations
            if (Input.StartDate.HasValue && Input.EndDate.HasValue)
            {
                if (Input.EndDate < Input.StartDate)
                    ModelState.AddModelError("Input.EndDate", "La fecha de finalización no puede ser anterior al inicio.");
            }

            var equipmentUnit = await _context.EquipmentUnits.Include(u => u.Equipment).FirstOrDefaultAsync(u => u.Id == Input.EquipmentUnitId);
            if (equipmentUnit == null) 
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "La unidad física no existe.");
            }
            else if (equipmentUnit.CurrentStatus == EquipmentStatus.OnLoan)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "No se puede realizar mantenimiento a un equipo que actualmente está en préstamo.");
            }

            // Sanitize CostDetails
            if (Input.CostDetails != null)
                Input.CostDetails = Input.CostDetails.Where(d => !string.IsNullOrWhiteSpace(d.Concept)).ToList();

            decimal totalCosts = Input.CostDetails?.Sum(d => d.Subtotal) ?? 0;
            Input.ActualCost = totalCosts;

            if (!ModelState.IsValid)
            {
                LoadLists();
                return Page();
            }

            try 
            {
                var maintenance = new Maintenance
                {
                    EquipmentUnitId = Input.EquipmentUnitId,
                    MaintenanceTypeId = Input.MaintenanceTypeId,
                    TechnicianId = Input.TechnicianId,
                    RequestId = Input.RequestId,
                    Description = Input.Description.Clean(),
                    ScheduledDate = Input.ScheduledDate,
                    StartDate = Input.StartDate,
                    EndDate = Input.EndDate,
                    ActualCost = Input.ActualCost,
                    Observations = Input.Observations?.Clean(),
                    Recommendations = Input.Recommendations?.Clean(),
                    Status = Input.Status,
                    CostDetails = Input.CostDetails ?? new(),
                    CreatedDate = DateTime.Now
                };

                var currentUser = await _userManager.GetUserAsync(User);
                maintenance.CreatedById = currentUser?.Id;

                _context.Maintenances.Add(maintenance);
                
                // Actualizar estado de la unidad física a En Mantenimiento y registrar historial
                if (equipmentUnit != null && equipmentUnit.CurrentStatus != EquipmentStatus.UnderMaintenance)
                {
                    // 1. Cerrar historial anterior
                    var lastHistory = await _context.EquipmentStateHistories
                        .Where(h => h.EquipmentUnitId == equipmentUnit.Id && h.EndDate == null)
                        .OrderByDescending(h => h.StartDate)
                        .FirstOrDefaultAsync();
                    
                    if (lastHistory != null)
                    {
                        lastHistory.EndDate = DateTime.Now;
                        _context.EquipmentStateHistories.Update(lastHistory);
                    }

                    // 2. Crear nuevo historial
                    var newHistory = new EquipmentStateHistory
                    {
                        EquipmentUnitId = equipmentUnit.Id,
                        Status = EquipmentStatus.UnderMaintenance,
                        StartDate = DateTime.Now,
                        Reason = "Ingreso a proceso de mantenimiento."
                    };
                    _context.EquipmentStateHistories.Add(newHistory);

                    // 3. Actualizar estado
                    equipmentUnit.CurrentStatus = EquipmentStatus.UnderMaintenance;
                    _context.EquipmentUnits.Update(equipmentUnit);
                }

                await _context.SaveChangesAsync();

                TempData.Success($"Mantenimiento para '{equipmentUnit?.Equipment?.Name}' guardado correctamente.");
                
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData.Error($"Error al guardar el registro: {ex.Message}");
                LoadLists();
                return Page();
            }
        }

        private void LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name), "Id", "Name");
            
            ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            var technicians = _context.People
                .Where(p => p.Status == GeneralStatus.Activo)
                .Select(p => new { Id = p.Id, FullName = p.FullName })
                .ToList();
            ViewData["TechnicianId"] = new SelectList(technicians, "Id", "FullName");

            ViewData["MaintenanceTypeId"] = new SelectList(_context.MaintenanceTypes.OrderBy(mt => mt.Name), "Id", "Name");
            var requests = _context.Requests
                .Include(r => r.Laboratory)
                .OrderByDescending(r => r.CreatedDate)
                .Take(20)
                .ToList()
                .Select(r => new { 
                    Id = r.Id, 
                    DisplayText = $"#{r.Id} - {r.Laboratory?.Name} ({r.CreatedDate:dd/MM}): " + (r.Description.Length > 40 ? r.Description.Substring(0, 40) + "..." : r.Description)
                });
            ViewData["RequestId"] = new SelectList(requests, "Id", "DisplayText");
        }
    }
}
