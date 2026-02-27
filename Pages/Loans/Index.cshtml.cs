using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Loans
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Loan> Loans { get;set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public LoanStatus? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Loans
                .Include(l => l.EquipmentUnit)
                .ThenInclude(u => u.Equipment)
                .Include(l => l.Borrower)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.ToLower();
                query = query.Where(l => 
                    l.EquipmentUnit!.InventoryNumber.ToLower().Contains(term) ||
                    l.EquipmentUnit!.Equipment!.Name.ToLower().Contains(term) ||
                    l.Borrower!.FullName.ToLower().Contains(term));
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(l => l.Status == StatusFilter.Value);
            }

            Loans = await query
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync();

            // Auto-update 'Vencido' status for display purposes (optional logic)
            foreach (var loan in Loans.Where(l => l.Status == LoanStatus.Active && l.EstimatedReturnDate < DateTime.Now))
            {
                loan.Status = LoanStatus.Overdue;
            }
        }
    }
}
