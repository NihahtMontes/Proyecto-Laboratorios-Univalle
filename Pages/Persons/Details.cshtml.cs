using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Persons
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Person Person { get; set; } = default!;
        public List<Maintenance> Maintenances { get; set; } = new();
        public List<Loan> Loans { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.People
                .Include(p => p.CreatedBy)
                .Include(p => p.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (person == null)
            {
                return NotFound();
            }

            Person = person;

            // Fetch related data
            Maintenances = await _context.Maintenances
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(u => u.Equipment)
                .Include(m => m.MaintenanceType)
                .Where(m => m.TechnicianId == id)
                .OrderByDescending(m => m.StartDate ?? m.CreatedDate)
                .Take(10)
                .ToListAsync();

            Loans = await _context.Loans
                .Include(l => l.EquipmentUnit)
                    .ThenInclude(u => u.Equipment)
                .Where(l => l.BorrowerId == id)
                .OrderByDescending(l => l.LoanDate)
                .Take(10)
                .ToListAsync();

            return Page();
        }
    }
}
