using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IManagementContextService _managementContext;

        public IndexModel(ApplicationDbContext context, IManagementContextService managementContext)
        {
            _context = context;
            _managementContext = managementContext;
        }

        // Inicializamos la lista para evitar errores de referencia nula en la vista
        public PaginatedList<Management> ManagementList { get; set; } = new PaginatedList<Management>(new List<Management>(), 0, 1, 20);

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        public async Task<IActionResult> OnGetAsync(int? pageIndex)
        {
            try
            {
                if (_context.Managements != null)
                {
                    ManagementList = await PaginatedList<Management>.CreateAsync(
                        _context.Managements
                            .Include(m => m.ManagementPlans)
                            .OrderByDescending(m => m.CreatedDate),
                        pageIndex ?? 1, 20);
                }
            }
            catch (Exception)
            {
                ManagementList = new PaginatedList<Management>(new List<Management>(), 0, 1, 20);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCloseManagementAsync(int id)
        {
            try
            {
                var management = await _context.Managements.FindAsync(id);
                if (management == null)
                {
                    return NotFound();
                }

                management.Status = Models.Enums.ManagementStatus.Completed;
                management.ActualClosedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _managementContext.InvalidateCache();

                TempData["Success"] = "La gestión administrativa ha sido cerrada (Terminada) exitosamente.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error de conexión: No se pudo cerrar la gestión.";
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDeleteLogicalAsync(int id)
        {
            try
            {
                var management = await _context.Managements.FindAsync(id);
                if (management == null)
                {
                    return NotFound();
                }

                if (management.Status == ManagementStatus.Active)
                {
                    TempData["Error"] = "No se puede eliminar una gestión que se encuentra ACTIVA actualmente.";
                    return RedirectToPage("./Index");
                }

                management.Status = ManagementStatus.Deleted;
                await _context.SaveChangesAsync();
                _managementContext.InvalidateCache();

                TempData["Success"] = "La gestión ha sido eliminada lógicamente del sistema.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error de conexión: No se pudo eliminar la gestión.";
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostSyncPlansAsync(int id)
        {
            var management = await _context.Managements.Include(m => m.ManagementPlans).AsTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (management == null) return NotFound();

            var currentUnitIds = management.ManagementPlans.Select(p => p.EquipmentUnitId).ToList();
                
            var missingUnits = await _context.EquipmentUnits
                .Where(u => u.CurrentStatus != EquipmentStatus.Deleted && !currentUnitIds.Contains(u.Id))
                .ToListAsync();

            if (!missingUnits.Any())
            {
                TempData["Success"] = "Todos los equipos activos ya se encuentran sincronizados con esta gestión.";
                return RedirectToPage("./Index");
            }

            foreach (var unit in missingUnits)
            {
                _context.ManagementPlans.Add(new ManagementPlan
                {
                    ManagementId = management.Id,
                    EquipmentUnitId = unit.Id,
                    CurrentPhase = WizardPhase.Verification,
                    CurrentState = WizardEquipmentState.PendingVerification,
                    PlanStatus = ManagementPlanStatus.Pending,
                    PlannedDate = null
                });
            }

            await _context.SaveChangesAsync();
            _managementContext.InvalidateCache();
            TempData["Success"] = $"Se han sincronizado {missingUnits.Count} nuevos equipos a la gestión {management.Code}.";
            return RedirectToPage("./Index");
        }
    }
}