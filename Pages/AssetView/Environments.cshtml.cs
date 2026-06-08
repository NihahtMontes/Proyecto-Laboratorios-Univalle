using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.AssetView
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class EnvironmentsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public EnvironmentsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public List<EnvironmentCard> Cards { get; set; } = new();
        private static readonly string[] CardColors =
        {
            "bg-primary",
            "bg-success",
            "bg-warning",
            "bg-danger",
            "bg-info",
            "bg-cyan",
            "bg-secondary"
        };

        public async Task OnGetAsync()
        {
            var laboratories = await _context.Laboratories
                .AsNoTracking()
                .Include(l => l.EquipmentUnits)
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Code)
                .ThenBy(l => l.Name)
                .ToListAsync();

            Cards = laboratories
                .Select((l, index) => new EnvironmentCard
                {
                    Id = l.Id,
                    DisplayName = LaboratoryDisplayHelper.Format(l),
                    Code = l.Code,
                    UnitCount = l.EquipmentUnits != null ? l.EquipmentUnits.Count : 0,
                    ColorClass = CardColors[index % CardColors.Length]
                })
                .ToList();
        }

        public class EnvironmentCard
        {
            public int Id { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string ColorClass { get; set; } = "bg-info";
            public int UnitCount { get; set; }
        }
    }
}
