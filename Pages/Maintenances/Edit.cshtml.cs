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
        public Maintenance Maintenance { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var maintenance = await _context.Maintenances
                .Include(m => m.CostDetails)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenance == null) return NotFound();
            
            Maintenance = maintenance;
            CargarListas();

            return Page();
        }

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

            // Quitar navegación de la validación
            ModelState.Remove("Maintenance.CreatedBy");
            ModelState.Remove("Maintenance.ModifiedBy");
            ModelState.Remove("Maintenance.EquipmentUnit");
            ModelState.Remove("Maintenance.MaintenanceType");
            ModelState.Remove("Maintenance.Technician");
            ModelState.Remove("Maintenance.Request");

            // Recalcular costo real basado en detalles
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

            var maintenanceDB = await _context.Maintenances
                .Include(m => m.CostDetails)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                .FirstOrDefaultAsync(m => m.Id == Maintenance.Id);

            if (maintenanceDB == null) return NotFound();

            // Auditoría y Actualización
            var currentUser = await _userManager.GetUserAsync(User);
            Maintenance.ModifiedById = currentUser?.Id;
            Maintenance.LastModifiedDate = DateTime.Now;

            _context.Entry(maintenanceDB).CurrentValues.SetValues(Maintenance);

            // Sincronización de Detalles de Costos
            // 1. Eliminar los que ya no están en la lista
            foreach (var existingDetail in maintenanceDB.CostDetails.ToList())
            {
                if (!Maintenance.CostDetails.Any(d => d.Id == existingDetail.Id))
                {
                    _context.Remove(existingDetail);
                }
            }

            // 2. Agregar nuevos o actualizar existentes
            foreach (var detailForm in Maintenance.CostDetails)
            {
                detailForm.MaintenanceId = Maintenance.Id;

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
                    DisplayName = $"{u.Equipment!.Name} (Inv: {u.InventoryNumber}) - [{u.Equipment!.EquipmentType!.Name}]"
                })
                .ToList();

            ViewData["EquipmentUnitId"] = new SelectList(equipos, "Id", "DisplayName");
            
            var tecnicos = _context.People
                .Where(p => p.Category == PersonCategory.Tecnico)
                .OrderBy(p => p.FirstName)
                .Select(p => new { Id = p.Id, FullName = p.FirstName + " " + p.LastName })
                .ToList();
            
            ViewData["TechnicianId"] = new SelectList(tecnicos, "Id", "FullName");
            ViewData["MaintenanceTypeId"] = new SelectList(_context.MaintenanceTypes.OrderBy(mt => mt.Name), "Id", "Name");
            ViewData["RequestId"] = new SelectList(_context.Requests.OrderByDescending(r => r.CreatedDate).Take(20), "Id", "Id");
        }

        private bool MaintenanceExists(int id)
        {
            return _context.Maintenances.Any(e => e.Id == id);
        }
    }
}
