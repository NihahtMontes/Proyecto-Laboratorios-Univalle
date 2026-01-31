using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

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
            CargarListas();

            Maintenance = new Maintenance
            {
                ScheduledDate = DateTime.Now,
                CostDetails = new List<CostDetail>()
            };

            return Page();
        }

        [BindProperty]
        public Maintenance Maintenance { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // Limpieza y Normalización
            Maintenance.Description = Maintenance.Description.Clean();
            Maintenance.Observations = Maintenance.Observations.Clean();
            Maintenance.Recommendations = Maintenance.Recommendations.Clean();

            // Validaciones Lógicas Rigurosas
            if (Maintenance.StartDate.HasValue && Maintenance.EndDate.HasValue)
            {
                if (Maintenance.EndDate < Maintenance.StartDate)
                {
                    ModelState.AddModelError("Maintenance.EndDate", NotificationHelper.Maintenances.EndDateBeforeStart);
                }
            }

            if (Maintenance.ScheduledDate.HasValue && Maintenance.StartDate.HasValue)
            {
                // Informativo: Opcionalmente podrías validar si inició después de lo programado, 
                // pero permitiremos flexibilidad técnica aquí.
            }

            // Quitar navegación de la validación
            ModelState.Remove("Maintenance.CreatedBy");
            ModelState.Remove("Maintenance.ModifiedBy");
            ModelState.Remove("Maintenance.Equipment");
            ModelState.Remove("Maintenance.MaintenanceType");
            ModelState.Remove("Maintenance.Technician");
            ModelState.Remove("Maintenance.Request");

            // Recalcular costo real basado en detalles (Doble chequeo de seguridad)
            decimal totalCosts = Maintenance.CostDetails?.Sum(d => d.Quantity * d.UnitPrice) ?? 0;
            Maintenance.ActualCost = totalCosts;

            if (Maintenance.Status == MaintenanceStatus.Completed && totalCosts <= 0)
            {
                ModelState.AddModelError("Maintenance.Status", NotificationHelper.Maintenances.CompletedWithoutCosts);
            }

            if (!ModelState.IsValid)
            {
                CargarListas();
                return Page();
            }

            try 
            {
                var currentUser = await _userManager.GetUserAsync(User);
                Maintenance.CreatedById = currentUser?.Id;
                Maintenance.CreatedDate = DateTime.Now;

                _context.Maintenances.Add(Maintenance);
                await _context.SaveChangesAsync();

                // Cargar Equipment para obtener el nombre
                var equipment = await _context.Equipments.FindAsync(Maintenance.EquipmentId);
                TempData.Success(NotificationHelper.Maintenances.Created(equipment?.Name));
                
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
            // Selector Pro de Equipos: Nombre (Inv: XXX) - Categoría
            var equipos = _context.Equipments
                .Include(e => e.EquipmentType)
                .Where(e => e.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(e => e.Name)
                .Select(e => new
                {
                    Id = e.Id,
                    DisplayName = $"{e.Name} (Inv: {e.InventoryNumber}) - [{e.EquipmentType.Name}]"
                })
                .ToList();

            ViewData["EquipmentId"] = new SelectList(equipos, "Id", "DisplayName");
            
            // Técnicos ordenados por nombre
            var tecnicos = _context.People
                .Where(p => p.Category == PersonCategory.Tecnico)
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .Select(p => new { Id = p.Id, FullName = p.FirstName + " " + p.LastName })
                .ToList();
            
            ViewData["TechnicianId"] = new SelectList(tecnicos, "Id", "FullName");

            // Tipos de Mantenimiento
            ViewData["MaintenanceTypeId"] = new SelectList(_context.MaintenanceTypes.OrderBy(mt => mt.Name), "Id", "Name");
            
            // Solicitudes (Opcional)
            ViewData["RequestId"] = new SelectList(_context.Requests.OrderByDescending(r => r.CreatedDate).Take(20), "Id", "Id");
        }
    }
}
