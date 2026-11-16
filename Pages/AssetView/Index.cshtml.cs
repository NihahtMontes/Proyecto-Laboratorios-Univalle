using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using System.Globalization;

namespace Proyecto_Laboratorios_Univalle.Pages.AssetView
{
    /// <summary>
    /// Inicio de la aplicación para Supervisores, Administradores y SuperAdmin:
    /// saludo, atajos por rol, navegación del inventario e indicadores (Chart.js).
    /// También responde en la ruta amiga «/Inicio» (AddPageRoute en Program.cs).
    /// </summary>
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class IndexModel : PageModel
    {
        private const string IndicadoresCacheKey = "inicio:indicadores";
        private static readonly TimeSpan IndicadoresCacheDuration = TimeSpan.FromMinutes(5);

        private readonly ApplicationDbContext _context;
        private readonly IManagementContextService _managementContext;
        private readonly SidebarCountsService _sidebarCounts;
        private readonly ICurrentUserService _currentUser;
        private readonly IMemoryCache _cache;

        public IndexModel(
            ApplicationDbContext context,
            IManagementContextService managementContext,
            SidebarCountsService sidebarCounts,
            ICurrentUserService currentUser,
            IMemoryCache cache)
        {
            _context = context;
            _managementContext = managementContext;
            _sidebarCounts = sidebarCounts;
            _currentUser = currentUser;
            _cache = cache;
        }

        // ══════════════════════════ Pestaña Resumen ══════════════════════════

        /// <summary>Saludo según la hora de Bolivia («Buenos días/tardes/noches»).</summary>
        public string Greeting { get; private set; } = "Buenos días";

        /// <summary>Nombre visible del usuario (mismo origen que UserProfileViewComponent).</summary>
        public string UserDisplayName { get; private set; } = string.Empty;

        /// <summary>Fecha de hoy en Bolivia, en texto largo es-BO.</summary>
        public string TodayText { get; private set; } = string.Empty;

        /// <summary>Administrador o SuperAdmin: mismo criterio de roles que el menú lateral.</summary>
        public bool IsAdminMenu { get; private set; }

        /// <summary>Solicitudes de prácticas de docentes pendientes de revisión.</summary>
        public int PendingTeacherRequests { get; private set; }

        /// <summary>Gestión correctiva activa: existe, código y solicitudes L7 pendientes de atención.</summary>
        public bool HasCorrective { get; private set; }
        public string? CorrectiveCode { get; private set; }
        public int CorrectivePendingRequests { get; private set; }

        /// <summary>Gestión preventiva activa (solo Administrador/SuperAdmin): código y avance de planes.</summary>
        public bool HasPreventive { get; private set; }
        public string? PreventiveCode { get; private set; }
        public int PreventivePlannedUnits { get; private set; }
        public int PreventiveCompletedUnits { get; private set; }

        // ══════════════════════════ Pestaña Inventario ══════════════════════════

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
        public int StockRecordsCount { get; set; }
        public int StockLaboratoriesCount { get; set; }
        /// <summary>Artículos del catálogo (utensilios, vajilla, cristalería, etc.); no son unidades patrimoniales.</summary>
        public int ArticlesCount { get; set; }
        public int ArticleCategoriesCount { get; set; }

        // ══════════════════════════ Pestañas ══════════════════════════

        [BindProperty(SupportsGet = true)]
        public string? ActiveTab { get; set; }

        public bool ShowInventoryTab => IsTab("Inventario");
        public bool ShowIndicatorsTab => IsTab("Indicadores");

        private bool IsTab(string name) => string.Equals(ActiveTab, name, StringComparison.OrdinalIgnoreCase);

        public async Task OnGetAsync()
        {
            LoadGreeting();
            await LoadUserDisplayNameAsync();

            IsAdminMenu = User.IsInRole(AuthorizationHelper.RoleAdministrator)
                || User.IsInRole(AuthorizationHelper.RoleSuperAdmin);

            // Atajos del Resumen: contadores vivos pero livianos (CountAsync + AsNoTracking).
            var (pendingRequests, _) = await _sidebarCounts.GetAsync(User);
            PendingTeacherRequests = pendingRequests;

            var corrective = await _managementContext.GetCurrentManagementAsync(ManagementType.Corrective);
            HasCorrective = corrective != null;
            CorrectiveCode = corrective?.Code;
            if (corrective != null)
            {
                CorrectivePendingRequests = await _context.Requests
                    .AsNoTracking()
                    .CountAsync(r => r.ManagementId == corrective.Id
                        && (r.Status == RequestStatus.Pending
                            || r.Status == RequestStatus.Scheduled
                            || r.Status == RequestStatus.InProgress));
            }

            if (IsAdminMenu)
            {
                var preventive = await _managementContext.GetCurrentManagementAsync(ManagementType.Preventive);
                HasPreventive = preventive != null;
                PreventiveCode = preventive?.Code;
                if (preventive != null)
                {
                    var plans = _context.ManagementPlans
                        .AsNoTracking()
                        .Where(p => p.ManagementId == preventive.Id);
                    PreventivePlannedUnits = await plans.CountAsync();
                    PreventiveCompletedUnits = await plans.CountAsync(p =>
                        p.PlanStatus == ManagementPlanStatus.Completed);
                }
            }

            await LoadInventoryCountsAsync();
        }

        private void LoadGreeting()
        {
            var boliviaNow = BoliviaTime.ToLocal(DateTime.UtcNow);
            var hour = boliviaNow.Hour;
            Greeting = hour < 12 ? "Buenos días" : hour < 19 ? "Buenas tardes" : "Buenas noches";
            TodayText = boliviaNow.Date.ToString("D", new CultureInfo("es-BO"));
            UserDisplayName = User.Identity?.Name ?? string.Empty;
        }

        /// <summary>Nombre del usuario como lo muestra el perfil (Users.FullName, [NotMapped]).</summary>
        private async Task LoadUserDisplayNameAsync()
        {
            if (_currentUser.UserId is int userId)
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    var fullName = user.FullName?.Trim();
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        UserDisplayName = fullName;
                        return;
                    }
                    if (!string.IsNullOrEmpty(user.UserName))
                    {
                        UserDisplayName = user.UserName;
                        return;
                    }
                }
            }
        }

        private async Task LoadInventoryCountsAsync()
        {
            var validEquipmentClassifications = EquipmentClassificationRules.EquipmentSubclassifications.ToArray();
            var validUtensilTypes = EquipmentClassificationRules.UtensilSubclassifications.ToArray();

            var activeUnits = ActiveUnitsQuery();

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
            StockRecordsCount = await _context.ArticleStocks
                .AsNoTracking()
                .CountAsync();
            StockLaboratoriesCount = await _context.ArticleStocks
                .AsNoTracking()
                .Select(s => s.LaboratoryId)
                .Distinct()
                .CountAsync();
            ArticlesCount = await _context.Articles
                .AsNoTracking()
                .CountAsync(a => a.Status != GeneralStatus.Eliminado);
            ArticleCategoriesCount = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Status != GeneralStatus.Eliminado && a.ArticleCategoryId.HasValue)
                .Select(a => a.ArticleCategoryId!.Value)
                .Distinct()
                .CountAsync();
            UnitsWithoutSpecificClassificationCount = PendingClassificationUnitsCount + LegacyInferredUnitsCount;
            ClassificationQualityPercent = TotalUnitsCount == 0
                ? 0
                : (int)Math.Round(((decimal)(TotalUnitsCount - UnitsWithoutSpecificClassificationCount) / TotalUnitsCount) * 100m);
        }

        // ══════════════════════════ Pestaña Indicadores ══════════════════════════

        /// <summary>
        /// Datos agregados de los gráficos de la pestaña Indicadores. Se carga solo al
        /// abrir la pestaña (fetch) y se cachea 5 minutos para no castigar el inicio.
        /// </summary>
        public async Task<JsonResult> OnGetIndicadoresAsync()
        {
            var payload = await _cache.GetOrCreateAsync(IndicadoresCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = IndicadoresCacheDuration;
                return await BuildIndicadoresAsync();
            });

            return new JsonResult(payload);
        }

        private async Task<InicioIndicadoresPayload> BuildIndicadoresAsync()
        {
            var validEquipmentClassifications = EquipmentClassificationRules.EquipmentSubclassifications.ToArray();
            var validUtensilTypes = EquipmentClassificationRules.UtensilSubclassifications.ToArray();
            var activeUnits = ActiveUnitsQuery();

            var category = await LoadCategoryDataAsync(activeUnits, validEquipmentClassifications, validUtensilTypes);
            var status = await LoadStatusDataAsync(activeUnits);
            var equipment = await LoadEquipmentClassificationDataAsync(activeUnits, validEquipmentClassifications);
            var utensil = await LoadUtensilClassificationDataAsync(activeUnits, validUtensilTypes);
            var other = await LoadOtherClassificationDataAsync(activeUnits);
            var condition = await LoadConditionDataAsync(activeUnits);
            var environment = await LoadEnvironmentDataAsync(activeUnits);

            return new InicioIndicadoresPayload
            {
                TotalUnits = await activeUnits.CountAsync(),
                GeneratedAt = BoliviaTime.ToLocal(DateTime.UtcNow).ToString("HH:mm", CultureInfo.InvariantCulture),
                Charts =
                {
                    BuildDonut("category", "¿Cómo se compone el inventario?", category, 5,
                        (items, total) =>
                        {
                            var top = items.OrderByDescending(p => p.Count).FirstOrDefault();
                            return top == null
                                ? "Sin datos todavía."
                                : $"El {PercentOf(top.Count, total)} % del inventario corresponde a {top.Label.ToLowerInvariant()}.";
                        }),
                    BuildDonut("status", "¿En qué estado están las unidades?", status, 5, BuildStatusConclusion),
                    BuildBar("equipment", "Equipos por clasificación técnica", equipment,
                        top => $"La clasificación con más equipos es {top.Label} ({top.Count})."),
                    BuildBar("utensil", "Utensilios por tipo", utensil,
                        top => $"El tipo más común es {top.Label} ({top.Count} unidades)."),
                    BuildBar("other", "Otros activos por clasificación", other,
                        top => $"La clasificación más frecuente es {top.Label} ({top.Count} unidades)."),
                    BuildBar("condition", "Condición física del inventario", condition,
                        top => $"La condición física más frecuente es {top.Label} ({top.Count} unidades)."),
                    BuildBar("environment", "Ambientes con más inventario", environment,
                        top => $"El ambiente con más unidades es {top.Label} ({top.Count}).")
                }
            };
        }

        private static string BuildStatusConclusion(List<ChartPoint> items, int total)
        {
            if (total <= 0)
            {
                return "Sin datos todavía.";
            }

            var operationalLabel = EnumHelper.GetDisplayName(EquipmentStatus.Operational);
            var operational = items.FirstOrDefault(p => p.Label == operationalLabel);
            return operational == null
                ? "Ninguna unidad está en estado operativo."
                : $"El {PercentOf(operational.Count, total)} % de las unidades está operativo.";
        }

        private static InicioChartPayload BuildDonut(
            string key,
            string title,
            List<ChartPoint> points,
            int maxSlices,
            Func<List<ChartPoint>, int, string> conclusion)
        {
            var capped = Cap(points.Where(p => p.Count > 0).ToList(), maxSlices);
            var total = capped.Sum(p => p.Count);
            return new InicioChartPayload
            {
                Key = key,
                Type = "donut",
                Title = title,
                Conclusion = conclusion(capped, total),
                Total = total,
                Items = capped.Select(ToPoint).ToList()
            };
        }

        private static InicioChartPayload BuildBar(
            string key,
            string title,
            List<ChartPoint> points,
            Func<ChartPoint, string> conclusion)
        {
            var capped = Cap(points.Where(p => p.Count > 0).ToList(), 8);
            // La conclusión nombra una categoría real, nunca el grupo «Demás» que junta lo que no entra.
            var top = capped.Where(p => p.Label != RestLabel).OrderByDescending(p => p.Count).FirstOrDefault();
            return new InicioChartPayload
            {
                Key = key,
                Type = "bar",
                Title = title,
                Conclusion = top == null ? "Sin datos todavía." : conclusion(top),
                Total = capped.Sum(p => p.Count),
                Items = capped.Select(ToPoint).ToList()
            };
        }

        private static InicioChartPoint ToPoint(ChartPoint point)
        {
            return new InicioChartPoint { Label = point.Label, Value = point.Count };
        }

        /// <summary>Etiqueta del grupo que junta las categorías que no entran (no se confunde con la categoría real «Otro»).</summary>
        private const string RestLabel = "Demás categorías";

        /// <summary>Deja las <paramref name="max"/> categorías más grandes y agrupa el resto en «Demás categorías».</summary>
        private static List<ChartPoint> Cap(List<ChartPoint> points, int max)
        {
            var ordered = points.OrderByDescending(p => p.Count).ToList();
            if (ordered.Count <= max)
            {
                return ordered;
            }

            var head = ordered.Take(max).ToList();
            var rest = ordered.Skip(max).Sum(p => p.Count);
            if (rest > 0)
            {
                head.Add(new ChartPoint(RestLabel, rest));
            }
            return head;
        }

        private static string PercentOf(int part, int total)
        {
            return total <= 0
                ? "0"
                : ((int)Math.Round(part * 100m / total)).ToString(CultureInfo.InvariantCulture);
        }

        private IQueryable<Proyecto_Laboratorios_Univalle.Models.EquipmentUnit> ActiveUnitsQuery()
        {
            return _context.EquipmentUnits
                .AsNoTracking()
                .Include(u => u.Equipment)
                .Include(u => u.Laboratory)
                .Where(u => u.Equipment != null
                    && u.CurrentStatus != EquipmentStatus.Deleted
                    && u.Equipment!.Status != GeneralStatus.Eliminado);
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
                .Select(g => new ChartPoint(g.Label.HasValue ? Display(g.Label.Value) : "Sin condición", g.Count))
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
                .ToListAsync();

            return data.Select(g => new ChartPoint(FormatEnvironment(g.Code, g.Name), g.Count)).ToList();
        }

        private sealed record ChartPoint(string Label, int Count);
    }

    /// <summary>Respuesta de <c>OnGetIndicadoresAsync</c>: serializada en camelCase por MVC.</summary>
    public sealed class InicioIndicadoresPayload
    {
        public int TotalUnits { get; set; }
        public string GeneratedAt { get; set; } = string.Empty;
        public List<InicioChartPayload> Charts { get; set; } = new();
    }

    public sealed class InicioChartPayload
    {
        public string Key { get; set; } = string.Empty;
        public string Type { get; set; } = "bar";
        public string Title { get; set; } = string.Empty;
        public string Conclusion { get; set; } = string.Empty;
        public int Total { get; set; }
        public List<InicioChartPoint> Items { get; set; } = new();
    }

    public sealed class InicioChartPoint
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
