using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Maintenance> Maintenances { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Maintenances = await _context.Maintenances
                .Include(m => m.CreatedBy)
                .Include(m => m.Equipment)
                .Include(m => m.ModifiedBy)
                .Include(m => m.Request)
                .Include(m => m.Technician)
                .Include(m => m.MaintenanceType).ToListAsync();
        }
    }
}
