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
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El equipo es obligatorio")]
            [Display(Name = "Equipo Objetivo")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "El tipo de mantenimiento es obligatorio")]
            [Display(Name = "Tipo de Servicio")]
            public int MaintenanceTypeId { get; set; }

            [Display(Name = "Técnico Responsable")]
            public int? TechnicianId { get; set; }

            [Required]
            [Display(Name = "Fecha Programada")]
            [DataType(DataType.Date)]
            public DateTime? ScheduledDate { get; set; }

            [Display(Name = "Inicio Real")]
            [DataType(DataType.DateTime)]
            public DateTime? StartDate { get; set; }

            [Display(Name = "Finalización")]
            [DataType(DataType.DateTime)]
            public DateTime? EndDate { get; set; }

            [Required]
            [Display(Name = "Estado del Proceso")]
            public MaintenanceStatus Status { get; set; }

            [Required(ErrorMessage = "La descripción es obligatoria")]
            [Display(Name = "Descripción del Trabajo / Requerimiento")]
            public string Description { get; set; } = string.Empty;

            [Display(Name = "Nivel de Satisfacción")]
            public MaintenanceSatisfaction? SatisfactionLevel { get; set; }

            [Display(Name = "Recomendaciones Post-Servicio")]
            public string? Recommendations { get; set; }

            [Display(Name = "Observaciones Internas")]
            public string? Observations { get; set; }

            [Display(Name = "Costo Real Total")]
            public decimal ActualCost { get; set; }

            public List<CostDetail> CostDetails { get; set; } = new();
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var maintenance = await _context.Maintenances
                .Include(m => m.CostDetails)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenance == null) return NotFound();
            
            Input = new InputModel
            {
                Id = maintenance.Id,
                EquipmentUnitId = maintenance.EquipmentUnitId,
                MaintenanceTypeId = maintenance.MaintenanceTypeId,
                TechnicianId = maintenance.TechnicianId,
                ScheduledDate = maintenance.ScheduledDate,
                StartDate = maintenance.StartDate,
                EndDate = maintenance.EndDate,
                Status = maintenance.Status,
                Description = maintenance.Description,
                SatisfactionLevel = maintenance.SatisfactionLevel,
                Recommendations = maintenance.Recommendations,
                Observations = maintenance.Observations,
                ActualCost = maintenance.ActualCost ?? 0m,
                CostDetails = maintenance.CostDetails.ToList()
            };

            CargarListas();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Limpieza y Normalización
            Input.Description = Input.Description.Clean();
            Input.Observations = Input.Observations.Clean();
            Input.Recommendations = Input.Recommendations.Clean();

            // Validaciones Lógicas Rigurosas
            if (Input.StartDate.HasValue && Input.EndDate.HasValue)
            {
                if (Input.EndDate < Input.StartDate)
                {
                    ModelState.AddModelError("Input.EndDate", NotificationHelper.Maintenances.EndDateBeforeStart);
                }
            }

            // Recalcular costo real basado en detalles
            decimal totalCosts = Input.CostDetails?.Sum(d => d.Quantity * d.UnitPrice) ?? 0;
            Input.ActualCost = totalCosts;

            if (Input.Status == MaintenanceStatus.Completed && totalCosts <= 0)
            {
                ModelState.AddModelError("Input.Status", NotificationHelper.Maintenances.CompletedWithoutCosts);
            }

            if (!ModelState.IsValid)
            {
                CargarListas();
                return Page();
            }

            var maintenanceDB = await _context.Maintenances
                .Include(m => m.CostDetails)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                .FirstOrDefaultAsync(m => m.Id == Input.Id);

            if (maintenanceDB == null) return NotFound();

            // Actualizar campos desde InputModel (Protegemos propiedades de navegación de Overposting)
            maintenanceDB.EquipmentUnitId = Input.EquipmentUnitId;
            maintenanceDB.MaintenanceTypeId = Input.MaintenanceTypeId;
            maintenanceDB.TechnicianId = Input.TechnicianId;
            maintenanceDB.ScheduledDate = Input.ScheduledDate;
            maintenanceDB.StartDate = Input.StartDate;
            maintenanceDB.EndDate = Input.EndDate;
            maintenanceDB.Status = Input.Status;
            maintenanceDB.Description = Input.Description;
            maintenanceDB.SatisfactionLevel = Input.SatisfactionLevel;
            maintenanceDB.Recommendations = Input.Recommendations;
            maintenanceDB.Observations = Input.Observations;
            maintenanceDB.ActualCost = Input.ActualCost;

            if (Input.CostDetails != null)
            {
                Input.CostDetails = Input.CostDetails.Where(d => !string.IsNullOrWhiteSpace(d.Concept)).ToList();
            }
            else
            {
                Input.CostDetails = new List<CostDetail>();
            }

            foreach (var existingDetail in maintenanceDB.CostDetails.ToList())
            {
                if (!Input.CostDetails.Any(d => d.Id == existingDetail.Id))
                {
                    _context.Remove(existingDetail);
                }
            }

            foreach (var detailForm in Input.CostDetails)
            {
                detailForm.MaintenanceId = maintenanceDB.Id;

                var existingDetail = maintenanceDB.CostDetails.FirstOrDefault(d => d.Id == detailForm.Id && d.Id != 0);
                if (existingDetail != null)
                {
                    _context.Entry(existingDetail).CurrentValues.SetValues(detailForm);
                }
                else
                {
                    maintenanceDB.CostDetails.Add(detailForm);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success(NotificationHelper.Maintenances.Updated(maintenanceDB.EquipmentUnit?.Equipment?.Name));
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData.Error(NotificationHelper.Maintenances.SaveError(ex.Message));
                CargarListas();
                return Page();
            }
        }

        private void CargarListas()
        {
            var equipos = _context.EquipmentUnits
                .Include(u => u.Equipment)
                    .ThenInclude(e => e.EquipmentType)
                .Where(u => u.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(u => u.Equipment!.Name)
                .Select(u => new
                {
                    Id = u.Id,
                    DisplayName = $"{u.Equipment!.Name} (Inv: {u.InventoryNumber}) - {(u.Equipment.Brand ?? "S/M")} {(u.Equipment.Model ?? "S/M")} [S/N: {u.SerialNumber ?? "N/A"}] - [{u.Equipment!.EquipmentType!.Name}]"
                })
                .ToList();

            ViewData["EquipmentUnitId"] = new SelectList(equipos, "Id", "DisplayName");
            
            var tecnicos = _context.People
                .Where(p => p.Status == GeneralStatus.Activo)
                .OrderBy(p => p.Id)
                .AsEnumerable()
                .Select(p => new { Id = p.Id, FullName = p.FullName })
                .ToList();
            
            ViewData["TechnicianId"] = new SelectList(tecnicos, "Id", "FullName");
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
