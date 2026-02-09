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
            LoadLists();

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
            // Data Cleaning and Normalization
            Maintenance.Description = Maintenance.Description.Clean();
            Maintenance.Observations = Maintenance.Observations.Clean();
            Maintenance.Recommendations = Maintenance.Recommendations.Clean();

            // Rigorous Technical Validations
            if (Maintenance.StartDate.HasValue && Maintenance.EndDate.HasValue)
            {
                if (Maintenance.EndDate < Maintenance.StartDate)
                {
                    ModelState.AddModelError("Maintenance.EndDate", "La fecha de finalización no puede ser anterior al inicio.");
                }
            }

            // Remove navigation properties from validation
            ModelState.Remove("Maintenance.CreatedBy");
            ModelState.Remove("Maintenance.ModifiedBy");
            ModelState.Remove("Maintenance.EquipmentUnit");
            ModelState.Remove("Maintenance.MaintenanceType");
            ModelState.Remove("Maintenance.Technician");
            ModelState.Remove("Maintenance.Request");

            // Sanitize CostDetails list (remove rows with empty descriptions)
            if (Maintenance.CostDetails != null)
            {
                Maintenance.CostDetails = Maintenance.CostDetails.Where(d => !string.IsNullOrWhiteSpace(d.Description)).ToList();
            }

            // Recalculate actual cost based on details (Double security check)
            decimal totalCosts = Maintenance.CostDetails?.Sum(d => d.Quantity * d.UnitPrice) ?? 0;
            Maintenance.ActualCost = totalCosts;

            if (Maintenance.Status == MaintenanceStatus.Completed && totalCosts <= 0)
            {
                // Note: This could be a warning, but for strict laboratorios we might want it as an error
                // ModelState.AddModelError("Maintenance.Status", "Un mantenimiento completado usualmente requiere registro de insumos o mano de obra.");
            }

            if (!ModelState.IsValid)
            {
                LoadLists();
                return Page();
            }

            try 
            {
                var currentUser = await _userManager.GetUserAsync(User);
                Maintenance.CreatedById = currentUser?.Id;
                Maintenance.CreatedDate = DateTime.Now;

                _context.Maintenances.Add(Maintenance);
                await _context.SaveChangesAsync();

                // Get equipment name for notification
                var equipmentUnit = await _context.EquipmentUnits.Include(u => u.Equipment).FirstOrDefaultAsync(u => u.Id == Maintenance.EquipmentUnitId);
                TempData.Success($"Mantenimiento para '{equipmentUnit?.Equipment?.Name}' programado correctamente.");
                
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
            // Equipment Selector: Name (Inv: XXX) - Category
            var equipments = _context.EquipmentUnits
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

            ViewData["EquipmentUnitId"] = new SelectList(equipments, "Id", "DisplayName");
            
            // Technicians sorted by name
            var technicians = _context.People
                .Where(p => p.Category == PersonCategory.Tecnico)
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .Select(p => new { Id = p.Id, FullName = p.FirstName + " " + p.LastName })
                .ToList();
            
            ViewData["TechnicianId"] = new SelectList(technicians, "Id", "FullName");

            // Maintenance Types
            ViewData["MaintenanceTypeId"] = new SelectList(_context.MaintenanceTypes.OrderBy(mt => mt.Name), "Id", "Name");
            
            // Recent Requests (Optional)
            ViewData["RequestId"] = new SelectList(_context.Requests.OrderByDescending(r => r.CreatedDate).Take(20), "Id", "Id");
        }
    }
}
