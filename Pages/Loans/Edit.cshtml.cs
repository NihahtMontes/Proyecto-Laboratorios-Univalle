using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Loans
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LoanEditModel Input { get; set; } = new();

        public string BorrowerName { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;

        public class LoanEditModel
        {
            public int Id { get; set; }

            [Required]
            [DataType(DataType.DateTime)]
            [Display(Name = "Fecha de Préstamo")]
            public DateTime LoanDate { get; set; }

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Devolución Estimada")]
            public DateTime EstimatedReturnDate { get; set; }

            [Display(Name = "Observaciones de Salida")]
            public string? DepartureObservations { get; set; }

            // Return Section
            [Display(Name = "Marcar como Devuelto")]
            public bool IsReturned { get; set; }

            [DataType(DataType.DateTime)]
            [Display(Name = "Fecha de Devolución Real")]
            public DateTime? ActualReturnDate { get; set; }

            [Display(Name = "Observaciones de Devolución")]
            public string? ReturnObservations { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var loan = await _context.Loans
                .Include(l => l.EquipmentUnit)
                .ThenInclude(u => u.Equipment)
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (loan == null) return NotFound();

            BorrowerName = loan.Borrower.FullName;
            EquipmentName = loan.EquipmentUnit.Equipment.Name;

            Input = new LoanEditModel
            {
                Id = loan.Id,
                LoanDate = loan.LoanDate,
                EstimatedReturnDate = loan.EstimatedReturnDate,
                DepartureObservations = loan.DepartureObservations,
                IsReturned = loan.Status == LoanStatus.Returned,
                ActualReturnDate = loan.ActualReturnDate ?? DateTime.Now,
                ReturnObservations = loan.ReturnObservations
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var loan = await _context.Loans
                .Include(l => l.EquipmentUnit)
                .FirstOrDefaultAsync(l => l.Id == Input.Id);

            if (loan == null) return NotFound();

            // Update basic info
            loan.LoanDate = Input.LoanDate;
            loan.EstimatedReturnDate = Input.EstimatedReturnDate;
            loan.DepartureObservations = Input.DepartureObservations;

            // Handle Return Logic
            if (Input.IsReturned && loan.Status != LoanStatus.Returned)
            {
                loan.Status = LoanStatus.Returned;
                loan.ActualReturnDate = Input.ActualReturnDate ?? DateTime.Now;
                loan.ReturnObservations = Input.ReturnObservations;

                // Set Unit back to Operational
                if (loan.EquipmentUnit != null)
                {
                    loan.EquipmentUnit.CurrentStatus = EquipmentStatus.Operational;
                    _context.EquipmentUnits.Update(loan.EquipmentUnit);
                }
            }
            else if (!Input.IsReturned && loan.Status == LoanStatus.Returned)
            {
                // Reverting a return (Edge case, maybe admin only?)
                loan.Status = LoanStatus.Active; // Or check overdue
                loan.ActualReturnDate = null;
                
                // Set Unit back to OnLoan
                if (loan.EquipmentUnit != null)
                {
                    loan.EquipmentUnit.CurrentStatus = EquipmentStatus.OnLoan;
                    _context.EquipmentUnits.Update(loan.EquipmentUnit);
                }
            }
            else if (loan.Status == LoanStatus.Active && loan.EstimatedReturnDate < DateTime.Now)
            {
                 // Auto update to Overdue if not returned and past date
                 // logic handled in Index mostly, but good to keep consistency if saved
            }

            // Save Observations if just editing an already returned loan
            if (loan.Status == LoanStatus.Returned)
            {
                loan.ReturnObservations = Input.ReturnObservations;
                if(Input.ActualReturnDate.HasValue) loan.ActualReturnDate = Input.ActualReturnDate.Value;
            }

            await _context.SaveChangesAsync();
            
            TempData.Success(NotificationHelper.Loans.Updated());

            return RedirectToPage("./Index");
        }
    }
}
