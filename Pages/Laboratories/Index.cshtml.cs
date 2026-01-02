using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Laboratories
{
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Laboratory> Laboratories { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Laboratories = await _context.Laboratories.Where(l => l.Status != GeneralStatus.Deleted)
                .Include(l => l.CreatedBy)
                .Include(l => l.Faculty)
                .Include(l => l.ModifiedBy).ToListAsync();
        }
    }
}
