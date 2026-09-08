using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.AssetView
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class EquipmentClassificationsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public EquipmentClassificationsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ClassificationCard> Cards { get; set; } = new();

        public async Task OnGetAsync()
        {
            var validClassifications = EquipmentClassificationRules.EquipmentSubclassifications.ToArray();

            var counts = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.Equipment != null &&
                            u.CurrentStatus != EquipmentStatus.Deleted &&
                            u.Equipment.Status != GeneralStatus.Eliminado &&
                            u.Equipment.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                            u.Equipment.Category == EquipmentCategory.Equipment &&
                            u.Equipment.TypeClassification.HasValue &&
                            validClassifications.Contains(u.Equipment.TypeClassification.Value))
                .GroupBy(u => u.Equipment!.TypeClassification)
                .Select(g => new { Classification = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Classification!.Value, x => x.Count);

            Cards = GetCards()
                .Select(card =>
                {
                    card.Count = counts.TryGetValue(card.Classification, out var count) ? count : 0;
                    return card;
                })
                .ToList();
        }

        private static List<ClassificationCard> GetCards() => new()
        {
            new("Equipos de calor", EquipmentTypeClassification.Calor, "bg-danger", "fas fa-temperature-high"),
            new("Equipos de frio", EquipmentTypeClassification.Frio, "bg-info", "fas fa-snowflake"),
            new("Equipos de congelacion", EquipmentTypeClassification.Congelacion, "bg-primary", "fas fa-icicles"),
            new("Equipos de ultracongelacion", EquipmentTypeClassification.Ultracongelacion, "bg-cyan", "fas fa-temperature-low"),
            new("Maquinas rotativas", EquipmentTypeClassification.MaquinasRotativas, "bg-warning", "fas fa-sync-alt"),
            new("Equipos electronicos (balanzas)", EquipmentTypeClassification.Electronico, "bg-success", "fas fa-microchip"),
            new("Equipos de seguridad industrial", EquipmentTypeClassification.SeguridadIndustrial, "bg-danger", "fas fa-shield-alt"),
            new("Equipos de medicion PCC", EquipmentTypeClassification.Medicion, "bg-info", "fas fa-tachometer-alt"),
            new("Equipos audiovisuales", EquipmentTypeClassification.Audiovisuales, "bg-primary", "fas fa-video"),
            new("Equipos electricos", EquipmentTypeClassification.Electricos, "bg-warning", "fas fa-bolt"),
            new("Mobiliario", EquipmentTypeClassification.Mobiliario, "bg-secondary", "fas fa-chair")
        };

        public class ClassificationCard
        {
            public ClassificationCard(string title, EquipmentTypeClassification classification, string colorClass, string iconClass)
            {
                Title = title;
                Classification = classification;
                ColorClass = colorClass;
                IconClass = iconClass;
            }

            public string Title { get; }
            public EquipmentTypeClassification Classification { get; }
            public string ColorClass { get; }
            public string IconClass { get; }
            public int Count { get; set; }
        }
    }
}
