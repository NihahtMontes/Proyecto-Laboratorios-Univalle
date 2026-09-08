using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.AssetView
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class UtensilClassificationsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public UtensilClassificationsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public List<UtensilCard> Cards { get; set; } = new();

        public async Task OnGetAsync()
        {
            var validUtensilTypes = EquipmentClassificationRules.UtensilSubclassifications.ToArray();

            var counts = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.Equipment != null &&
                            u.CurrentStatus != EquipmentStatus.Deleted &&
                            u.Equipment.Status != GeneralStatus.Eliminado &&
                            u.Equipment.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                            u.Equipment.Category == EquipmentCategory.Utensil &&
                            u.Equipment.UtensilType.HasValue &&
                            validUtensilTypes.Contains(u.Equipment.UtensilType.Value))
                .GroupBy(u => u.Equipment!.UtensilType)
                .Select(g => new { UtensilType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UtensilType!.Value, x => x.Count);

            Cards = GetCards()
                .Select(card =>
                {
                    card.Count = counts.TryGetValue(card.UtensilType, out var count) ? count : 0;
                    return card;
                })
                .ToList();
        }

        private static List<UtensilCard> GetCards() => new()
        {
            new("Menaje de cocina", UtensilType.MenajeCocina, "bg-primary", "fas fa-blender"),
            new("Bartending y Barismo", UtensilType.BartendingBarismo, "bg-success", "fas fa-cocktail"),
            new("Panaderia, Reposteria y Pasteleria", UtensilType.PanaderiaReposteriaPasteleria, "bg-warning", "fas fa-bread-slice"),
            new("Servicio", UtensilType.Servicio, "bg-info", "fas fa-concierge-bell"),
            new("Manteleria", UtensilType.Manteleria, "bg-danger", "fas fa-border-style"),
            new("Vajilla en general", UtensilType.VajillaGeneral, "bg-cyan", "fas fa-utensils")
        };

        public class UtensilCard
        {
            public UtensilCard(string title, UtensilType utensilType, string colorClass, string iconClass)
            {
                Title = title;
                UtensilType = utensilType;
                ColorClass = colorClass;
                IconClass = iconClass;
            }

            public string Title { get; }
            public UtensilType UtensilType { get; }
            public string ColorClass { get; }
            public string IconClass { get; }
            public int Count { get; set; }
        }
    }
}
