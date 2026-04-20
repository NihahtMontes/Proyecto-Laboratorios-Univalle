using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
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
        public EditInputModel Input { get; set; } = new();

        public List<VerificationCheckItem> CheckItems { get; set; } = [];

        public class EditInputModel
        {
            public int Id { get; set; }

            [Required]
            public int EquipmentUnitId { get; set; }

            [DataType(DataType.Date)]
            public DateTime Date { get; set; }

            public string? Observations { get; set; }

            public VerificationStatus Status { get; set; }

            public Dictionary<int, VerificationResult> Results { get; set; } = [];
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var verification = await _context.Verifications
                .Include(v => v.CheckResults)
                .Include(v => v.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (verification == null) return NotFound();

            // Mapear a InputModel
            Input = new EditInputModel
            {
                Id = verification.Id,
                EquipmentUnitId = verification.EquipmentUnitId,
                Date = verification.Date,
                Observations = verification.Observations,
                Status = verification.Status,
                Results = verification.CheckResults.ToDictionary(r => r.CheckItemId, r => r.Result)
            };

            LoadCheckItems();
            LoadLists();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadCheckItems();
                LoadLists();
                return Page();
            }

            var verification = await _context.Verifications
                .Include(v => v.CheckResults)
                .FirstOrDefaultAsync(v => v.Id == Input.Id);

            if (verification == null) return NotFound();

            // Actualizar metadata
            verification.Date = Input.Date;
            verification.Observations = Input.Observations;
            verification.Status = Input.Status;
            verification.EquipmentUnitId = Input.EquipmentUnitId;

            // Auditoría
            var user = await _userManager.GetUserAsync(User);
            if (user != null) verification.ModifiedById = user.Id;
            verification.LastModifiedDate = DateTime.UtcNow;

            // Actualizar resultados de checks
            foreach (var (checkItemId, result) in Input.Results)
            {
                var existingResult = verification.CheckResults.FirstOrDefault(r => r.CheckItemId == checkItemId);
                if (existingResult != null)
                {
                    existingResult.Result = result;
                }
                else
                {
                    _context.VerificationCheckResults.Add(new VerificationCheckResult
                    {
                        VerificationId = verification.Id,
                        CheckItemId = checkItemId,
                        Result = result
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success(NotificationHelper.Verifications.Updated(verification.Id));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VerificationExists(verification.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private void LoadCheckItems()
        {
            CheckItems = _context.VerificationCheckItems
                .Where(c => c.IsActive)
                .OrderBy(c => c.Order)
                .ToList();
        }

        private void LoadLists()
        {
            var units = _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Select(u => new { 
                    Id = u.Id, 
                    DisplayName = $"{u.Equipment!.Name} (INV: {u.InventoryNumber})" 
                })
                .ToList();

            ViewData["EquipmentUnitId"] = new SelectList(units, "Id", "DisplayName", Input.EquipmentUnitId);
        }

        private bool VerificationExists(int id)
        {
            return _context.Verifications.Any(e => e.Id == id);
        }
    }
}
