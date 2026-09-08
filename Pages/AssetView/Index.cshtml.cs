using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.Text.Json;

namespace Proyecto_Laboratorios_Univalle.Pages.AssetView
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalUnitsCount { get; set; }
        public int EquipmentUnitsCount { get; set; }
        public int UtensilUnitsCount { get; set; }
        public int OtherUnitsCount { get; set; }
        public int PendingClassificationUnitsCount { get; set; }
        public int LegacyInferredUnitsCount { get; set; }
        public int EnvironmentsCount { get; set; }
        public int EnvironmentsWithUnitsCount { get; set; }
        public int UnitsWithoutEnvironmentCount { get; set; }
        public int UnitsWithoutSerialCount { get; set; }
        public int UnitsWithoutSpecificClassificationCount { get; set; }
        public int ClassificationQualityPercent { get; set; }

        public string CategoryDataJson { get; set; } = "[]";
        public string EquipmentSubClassJson { get; set; } = "{}";
        public string UtensilSubClassJson { get; set; } = "{}";
        public string OtherSubClassJson { get; set; } = "{}";
        public string StatusDataJson { get; set; } = "[]";
        public string ConditionDataJson { get; set; } = "{}";
        public string EnvironmentDataJson { get; set; } = "{}";

        [BindProperty(SupportsGet = true)]
        public string? ActiveTab { get; set; }

        public bool ShowInventoryTab => string.Equals(ActiveTab, "Inventario", StringComparison.OrdinalIgnoreCase);

        public async Task OnGetAsync()
        {
            var validEquipmentClassifications = EquipmentClassificationRules.EquipmentSubclassifications.ToArray();
            var validUtensilTypes = EquipmentClassificationRules.UtensilSubclassifications.ToArray();

            var activeUnits = _context.EquipmentUnits
                .AsNoTracking()
                .Include(u => u.Equipment)
                .Include(u => u.Laboratory)
                .Where(u => u.Equipment != null
                    && u.CurrentStatus != EquipmentStatus.Deleted
                    && u.Equipment!.Status != GeneralStatus.Eliminado);

            TotalUnitsCount = await activeUnits.CountAsync();
            EquipmentUnitsCount = await activeUnits.CountAsync(u =>
                u.Equipment!.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                u.Equipment!.Category == EquipmentCategory.Equipment &&
                u.Equipment.TypeClassification.HasValue &&
                validEquipmentClassifications.Contains(u.Equipment.TypeClassification.Value));
            UtensilUnitsCount = await activeUnits.CountAsync(u =>
                u.Equipment!.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                u.Equipment!.Category == EquipmentCategory.Utensil &&
                u.Equipment.UtensilType.HasValue &&
                validUtensilTypes.Contains(u.Equipment.UtensilType.Value));
            OtherUnitsCount = await activeUnits.CountAsync(u =>
                u.Equipment!.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                u.Equipment.Category == EquipmentCategory.Other);
            PendingClassificationUnitsCount = await activeUnits.CountAsync(u =>
                u.Equipment!.ClassificationReviewStatus == EquipmentClassificationReviewStatus.PendingClient);
            LegacyInferredUnitsCount = await activeUnits.CountAsync(u =>
                u.Equipment!.ClassificationReviewStatus == EquipmentClassificationReviewStatus.LegacyInferred);
            EnvironmentsCount = await _context.Laboratories.AsNoTracking().CountAsync(l => l.Status == GeneralStatus.Activo);
            EnvironmentsWithUnitsCount = await activeUnits
                .Where(u => u.LaboratoryId.HasValue)
                .Select(u => u.LaboratoryId!.Value)
                .Distinct()
                .CountAsync();
            UnitsWithoutEnvironmentCount = await activeUnits.CountAsync(u => !u.LaboratoryId.HasValue);
            UnitsWithoutSerialCount = await activeUnits.CountAsync(u => u.SerialNumber == null || u.SerialNumber == "");
            UnitsWithoutSpecificClassificationCount = PendingClassificationUnitsCount + LegacyInferredUnitsCount;
            ClassificationQualityPercent = TotalUnitsCount == 0
                ? 0
                : (int)Math.Round(((decimal)(TotalUnitsCount - UnitsWithoutSpecificClassificationCount) / TotalUnitsCount) * 100m);

            CategoryDataJson = SerializeDonut(await LoadCategoryDataAsync(
                activeUnits,
                validEquipmentClassifications,
                validUtensilTypes));
            EquipmentSubClassJson = SerializeBar(await LoadEquipmentClassificationDataAsync(
                activeUnits,
                validEquipmentClassifications));
            UtensilSubClassJson = SerializeBar(await LoadUtensilClassificationDataAsync(
                activeUnits,
                validUtensilTypes));
            OtherSubClassJson = SerializeBar(await LoadOtherClassificationDataAsync(activeUnits));
            StatusDataJson = SerializeDonut(await LoadStatusDataAsync(activeUnits));
            ConditionDataJson = SerializeBar(await LoadConditionDataAsync(activeUnits));
            EnvironmentDataJson = SerializeBar(await LoadEnvironmentDataAsync(activeUnits));
        }

        private static string SerializeDonut(IEnumerable<ChartPoint> points)
        {
            return JsonSerializer.Serialize(points
                .Where(p => p.Count > 0)
                .Select(p => new object[] { p.Label, p.Count }));
        }

        private static string SerializeBar(IEnumerable<ChartPoint> points)
        {
            var rows = points.Where(p => p.Count > 0).ToList();
            var values = new List<object> { "Unidades" };
            values.AddRange(rows.Select(p => (object)p.Count));

            return JsonSerializer.Serialize(new
            {
                columns = rows.Count == 0 ? Array.Empty<object[]>() : new[] { values.ToArray() },
                categories = rows.Select(p => p.Label).ToArray()
            });
        }

        private static string Display(Enum value)
        {
            return EnumHelper.GetDisplayName(value);
        }

        private static string FormatEnvironment(string? code, string name)
        {
            return string.IsNullOrWhiteSpace(code) ? name : $"{code} - {name}";
        }

        private static async Task<List<ChartPoint>> LoadCategoryDataAsync(
            IQueryable<Proyecto_Laboratorios_Univalle.Models.EquipmentUnit> activeUnits,
            EquipmentTypeClassification[] validEquipmentClassifications,
            UtensilType[] validUtensilTypes)
        {
            var data = await activeUnits
                .Where(u => u.Equipment!.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed)
                .GroupBy(u =>
                    u.Equipment!.Category == EquipmentCategory.Equipment &&
                    u.Equipment.TypeClassification.HasValue &&
                    validEquipmentClassifications.Contains(u.Equipment.TypeClassification.Value)
                        ? EquipmentCategory.Equipment
                        : u.Equipment.Category == EquipmentCategory.Utensil &&
                          u.Equipment.UtensilType.HasValue &&
                          validUtensilTypes.Contains(u.Equipment.UtensilType.Value)
                            ? EquipmentCategory.Utensil
                            : EquipmentCategory.Other)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderBy(g => g.Label)
                .ToListAsync();

            return data.Select(g => new ChartPoint(Display(g.Label), g.Count)).ToList();
        }

        private static async Task<List<ChartPoint>> LoadEquipmentClassificationDataAsync(
            IQueryable<Proyecto_Laboratorios_Univalle.Models.EquipmentUnit> activeUnits,
            EquipmentTypeClassification[] validClassifications)
        {
            var data = await activeUnits
                .Where(u => u.Equipment!.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                            u.Equipment.Category == EquipmentCategory.Equipment &&
                            u.Equipment.TypeClassification.HasValue &&
                            validClassifications.Contains(u.Equipment.TypeClassification.Value))
                .GroupBy(u => u.Equipment!.TypeClassification)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Label)
                .ToListAsync();

            return data.Select(g => new ChartPoint(Display(g.Label!.Value), g.Count)).ToList();
        }

        private static async Task<List<ChartPoint>> LoadUtensilClassificationDataAsync(
            IQueryable<Proyecto_Laboratorios_Univalle.Models.EquipmentUnit> activeUnits,
            UtensilType[] validUtensilTypes)
        {
            var data = await activeUnits
                .Where(u => u.Equipment!.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                            u.Equipment.Category == EquipmentCategory.Utensil &&
                            u.Equipment.UtensilType.HasValue &&
                            validUtensilTypes.Contains(u.Equipment.UtensilType.Value))
                .GroupBy(u => u.Equipment!.UtensilType)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Label)
                .ToListAsync();

            return data.Select(g => new ChartPoint(Display(g.Label!.Value), g.Count)).ToList();
        }

        private static async Task<List<ChartPoint>> LoadOtherClassificationDataAsync(
            IQueryable<Proyecto_Laboratorios_Univalle.Models.EquipmentUnit> activeUnits)
        {
            var data = await activeUnits
                .Where(u => u.Equipment!.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                            u.Equipment.Category == EquipmentCategory.Other)
                .GroupBy(u => u.Equipment!.OtherClassificationDetail)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Label)
                .ToListAsync();

            return data.Select(g => new ChartPoint(g.Label ?? "Otro confirmado", g.Count)).ToList();
        }

        private static async Task<List<ChartPoint>> LoadStatusDataAsync(IQueryable<Proyecto_Laboratorios_Univalle.Models.EquipmentUnit> activeUnits)
        {
            var data = await activeUnits
                .GroupBy(u => u.CurrentStatus)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Label)
                .ToListAsync();

            return data.Select(g => new ChartPoint(Display(g.Label), g.Count)).ToList();
        }

        private static async Task<List<ChartPoint>> LoadConditionDataAsync(IQueryable<Proyecto_Laboratorios_Univalle.Models.EquipmentUnit> activeUnits)
        {
            var data = await activeUnits
                .GroupBy(u => u.PhysicalCondition)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            return data
                .Select(g => new ChartPoint(g.Label.HasValue ? Display(g.Label.Value) : "Sin condicion", g.Count))
                .ToList();
        }

        private static async Task<List<ChartPoint>> LoadEnvironmentDataAsync(IQueryable<Proyecto_Laboratorios_Univalle.Models.EquipmentUnit> activeUnits)
        {
            var data = await activeUnits
                .Where(u => u.Laboratory != null)
                .GroupBy(u => new { u.Laboratory!.Code, u.Laboratory.Name })
                .Select(g => new { g.Key.Code, g.Key.Name, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Code)
                .Take(8)
                .ToListAsync();

            return data.Select(g => new ChartPoint(FormatEnvironment(g.Code, g.Name), g.Count)).ToList();
        }

        private sealed record ChartPoint(string Label, int Count);
    }
}
