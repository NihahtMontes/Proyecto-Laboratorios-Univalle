using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Verification? Verification { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? LabId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Date { get; set; }

        public List<Verification> SessionEquipments { get; set; } = new();
        public string SessionLabName { get; set; } = string.Empty;
        public string SessionInspector { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (LabId.HasValue && Date.HasValue)
            {
                return await LoadSessionView();
            }

            if (id == null)
                return NotFound();

            var verification = await _context.Verifications
                .Include(v => v.CreatedBy)
                .Include(v => v.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(v => v.ModifiedBy)
                .Include(v => v.CheckResults)
                    .ThenInclude(r => r.CheckItem)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (verification == null)
                return NotFound();

            Verification = verification;
            return Page();
        }

        private async Task<IActionResult> LoadSessionView()
        {
            var sessionDate = Date.Value.Date;
            var nextDate = sessionDate.AddDays(1);

            var query = _context.Verifications
                .AsNoTracking()
                .Include(v => v.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Include(v => v.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                .Include(v => v.CreatedBy)
                .Where(v => v.EquipmentUnit != null
                         && v.EquipmentUnit.LaboratoryId == LabId.Value
                         && v.Date >= sessionDate
                         && v.Date < nextDate);

            SessionEquipments = await query.OrderBy(v => v.EquipmentUnit!.InventoryNumber).ToListAsync();

            if (SessionEquipments.Count == 0)
                return NotFound();

            var first = SessionEquipments.First();
            SessionDate = Date.Value;
            SessionLabName = first.EquipmentUnit?.Laboratory?.Name ?? "Laboratorio";
            SessionInspector = first.CreatedBy != null
                ? first.CreatedBy.FirstName + " " + first.CreatedBy.LastName
                : "Sistema";

            return Page();
        }
    }
}
