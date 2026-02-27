using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Career
{
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Proyecto_Laboratorios_Univalle.Models.Career> Careers { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Careers = await _context.Careers
                .Include(c => c.CreatedBy)
                .Include(c => c.Facultad)
                .Include(c => c.ModifiedBy).ToListAsync();
        }
    }
}
