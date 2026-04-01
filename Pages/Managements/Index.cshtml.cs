using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Helpers;
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

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Inicializamos la lista para evitar errores de referencia nula en la vista
        public IList<Management> ManagementList { get; set; } = new List<Management>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (_context.Managements != null)
                {
                    ManagementList = await _context.Managements
                        .Include(m => m.ManagementPlans)
                        .OrderByDescending(m => m.CreatedDate)
                        .ToListAsync();
                }
            }
            catch (Exception)
            {
                ManagementList = new List<Management>();
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

                TempData["Success"] = "La gestión ha sido eliminada lógicamente del sistema.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error de conexión: No se pudo eliminar la gestión.";
            }

            return RedirectToPage("./Index");
        }
    }
}