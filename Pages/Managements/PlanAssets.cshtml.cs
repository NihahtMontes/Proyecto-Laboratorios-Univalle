using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    [ValidateAntiForgeryToken]
    public class PlanAssetsModel : PageModel
    {
        private const int PageSize = 50;
        private readonly ApplicationDbContext _context;
        private readonly IManagementContextService _managementContext;
        private readonly IManagementPlanExclusionService _planExclusion;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<PlanAssetsModel> _logger;

        public PlanAssetsModel(
            ApplicationDbContext context,
            IManagementContextService managementContext,
            IManagementPlanExclusionService planExclusion,
            UserManager<User> userManager,
            ILogger<PlanAssetsModel> logger)
        {
            _context = context;
            _managementContext = managementContext;
            _planExclusion = planExclusion;
            _userManager = userManager;
            _logger = logger;
        }

        public Management Management { get; set; } = default!;
        public List<AssetCandidate> Candidates { get; set; } = new();
        public SelectList LaboratoryList { get; set; } = default!;
        public SelectList CategoryList { get; set; } = default!;
        public SelectList TypeClassificationList { get; set; } = default!;
        public SelectList UtensilTypeList { get; set; } = default!;

        public int TotalMatching { get; set; }
        public int AlreadyPlanned { get; set; }
        public int AvailableMatching { get; set; }
        public int WithHistory { get; set; }
        public int BlockedByClassificationCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int FirstItemNumber => TotalMatching == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
        public int LastItemNumber => Math.Min(CurrentPage * PageSize, TotalMatching);

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? LaboratoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public EquipmentCategory? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public EquipmentTypeClassification? TypeClassification { get; set; }

        [BindProperty(SupportsGet = true)]
        public UtensilType? UtensilType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty]
        public List<int> SelectedUnitIds { get; set; } = new();

        [BindProperty]
        public List<int> PageUnitIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (!await LoadManagementAsync())
                {
                    return RedirectToManagementError("No se encontro la gestion solicitada para planificar activos.");
                }

                await LoadListsAsync();
                await LoadCandidatesAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir planificacion de activos para gestion {ManagementId}", Id);
                return RedirectToManagementError("No se pudo abrir la planificación de activos. Intente nuevamente.");
            }
        }

        public async Task<IActionResult> OnPostApplySelectionAsync()
        {
            try
            {
                if (!await LoadManagementAsync())
                {
                    return RedirectToManagementError("No se encontro la gestion solicitada para aplicar la planificacion.");
                }

                if (!CanPlan())
                {
                    TempData.Warning("Solo se pueden planificar activos en una gestion preventiva activa.");
                    return RedirectToPage("./Details", new { id = Id, ActiveTab = "dashboard" });
                }

                await LoadCandidatesAsync();
                var pageUnitIds = PageUnitIds.Distinct().ToList();
                if (!pageUnitIds.Any())
                {
                    TempData.Warning("No hay activos visibles para aplicar cambios.");
                    return RedirectToCurrentPage();
                }

                var selectedIds = SelectedUnitIds
                    .Distinct()
                    .Where(pageUnitIds.Contains)
                    .ToList();

                var existingUnitIds = await _context.ManagementPlans
                    .AsNoTracking()
                    .Where(p => p.ManagementId == Id
                        && p.EquipmentUnitId.HasValue
                        && pageUnitIds.Contains(p.EquipmentUnitId.Value))
                    .Select(p => p.EquipmentUnitId!.Value)
                    .ToListAsync();

                var toAdd = selectedIds.Except(existingUnitIds).ToList();
                var toRemove = existingUnitIds.Except(selectedIds).ToList();

                if (!toAdd.Any() && !toRemove.Any())
                {
                    TempData.Info("No se detectaron cambios en la pagina visible.");
                    return RedirectToCurrentPage();
                }

                var currentUser = await _userManager.GetUserAsync(User);
                await using var transaction = await _context.Database.BeginTransactionAsync();

                foreach (var unitId in toAdd)
                {
                    _context.ManagementPlans.Add(new ManagementPlan
                    {
                        ManagementId = Id,
                        EquipmentUnitId = unitId,
                        CurrentPhase = WizardPhase.Verification,
                        CurrentState = WizardEquipmentState.PendingVerification,
                        PlanStatus = ManagementPlanStatus.Pending,
                        PlannedDate = null,
                        CreatedById = currentUser?.Id
                    });
                }

                var removedWithHistory = 0;
                foreach (var unitId in toRemove)
                {
                    var result = await _planExclusion.ExcludeUnitAsync(Id, unitId, currentUser?.Id);
                    if (result.Excluded && result.HadHistory)
                    {
                        removedWithHistory++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _managementContext.InvalidateCache();

                var removalMessage = toRemove.Any()
                    ? $" Se excluyeron {toRemove.Count} activo(s); {removedWithHistory} tenian historial y fueron cancelados logicamente."
                    : string.Empty;

                TempData.Success($"Planificacion actualizada: {toAdd.Count} activo(s) agregados.{removalMessage}");
                return RedirectToCurrentPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar planificacion de activos para gestion {ManagementId}", Id);
                return RedirectToManagementError("No se pudo actualizar la planificación de activos. Intente nuevamente.");
            }
        }

        private async Task<bool> LoadManagementAsync()
        {
            var management = await _context.Managements
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == Id);

            if (management == null)
            {
                return false;
            }

            Management = management;
            return true;
        }

        private bool CanPlan()
        {
            return Management.Type == ManagementType.Preventive && Management.Status == ManagementStatus.Active;
        }

        private async Task LoadListsAsync()
        {
            var labsQuery = _context.Laboratories
                .AsNoTracking()
                .Where(l => l.Status == GeneralStatus.Activo);

            if (Management.FacultyId.HasValue)
            {
                labsQuery = labsQuery.Where(l => l.FacultyId == Management.FacultyId.Value);
            }

            var labs = await labsQuery
                .OrderBy(l => l.Code)
                .ThenBy(l => l.Name)
                .ToListAsync();
            LaboratoryList = new SelectList(LaboratoryDisplayHelper.ToSelectItems(labs), "Id", "DisplayName", LaboratoryId);

            CategoryList = new SelectList(Enum.GetValues<EquipmentCategory>()
                .Select(v => new { Id = v, Name = EnumHelper.GetDisplayName(v) }), "Id", "Name", Category);

            TypeClassificationList = new SelectList(Enum.GetValues<EquipmentTypeClassification>()
                .Select(v => new { Id = v, Name = EnumHelper.GetDisplayName(v) }), "Id", "Name", TypeClassification);

            UtensilTypeList = new SelectList(Enum.GetValues<UtensilType>()
                .Where(v => v != Models.Enums.UtensilType.NoAplica)
                .Select(v => new { Id = v, Name = EnumHelper.GetDisplayName(v) }), "Id", "Name", UtensilType);
        }

        private async Task LoadCandidatesAsync()
        {
            var baseQuery = _context.EquipmentUnits
                .AsNoTracking()
                .Include(u => u.Equipment)
                .Include(u => u.Laboratory)
                .Where(u => u.CurrentStatus != EquipmentStatus.Deleted && u.Equipment != null);

            if (Management.FacultyId.HasValue)
            {
                baseQuery = baseQuery.Where(u => u.Laboratory != null && u.Laboratory.FacultyId == Management.FacultyId.Value);
            }

            BlockedByClassificationCount = await baseQuery.CountAsync(u =>
                u.Equipment!.ClassificationReviewStatus != EquipmentClassificationReviewStatus.Confirmed);

            baseQuery = baseQuery.Where(u =>
                u.Equipment!.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed);

            var query = ApplyFilters(baseQuery);

            TotalMatching = await query.CountAsync();
            TotalPages = TotalMatching == 0 ? 1 : (int)Math.Ceiling(TotalMatching / (double)PageSize);
            CurrentPage = Math.Clamp(PageIndex ?? 1, 1, TotalPages);

            var pageItems = await query
                .OrderBy(u => u.Equipment!.Category)
                .ThenBy(u => u.Equipment!.TypeClassification)
                .ThenBy(u => u.Equipment!.UtensilType)
                .ThenBy(u => u.Equipment!.Name)
                .ThenBy(u => u.InventoryNumber)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(u => new AssetCandidate
                {
                    UnitId = u.Id,
                    InventoryNumber = u.InventoryNumber,
                    EquipmentName = u.Equipment!.Name,
                    CategoryName = u.Equipment.Category == EquipmentCategory.Utensil && u.Equipment.UtensilType.HasValue
                        ? EnumHelper.GetDisplayName(u.Equipment.UtensilType.Value)
                        : u.Equipment.Category == EquipmentCategory.Equipment && u.Equipment.TypeClassification.HasValue
                            ? EnumHelper.GetDisplayName(u.Equipment.TypeClassification.Value)
                            : u.Equipment.OtherClassificationDetail ?? "Otro confirmado",
                    LaboratoryName = u.Laboratory == null
                        ? "Sin ambiente"
                        : (string.IsNullOrWhiteSpace(u.Laboratory.Code) ? u.Laboratory.Name : u.Laboratory.Code + " - " + u.Laboratory.Name)
                })
                .ToListAsync();

            var pageUnitIds = pageItems.Select(i => i.UnitId).ToList();
            var plannedUnitIds = await _context.ManagementPlans
                .AsNoTracking()
                .Where(p => p.ManagementId == Id && p.EquipmentUnitId.HasValue)
                .Select(p => p.EquipmentUnitId!.Value)
                .ToListAsync();

            var plannedSet = plannedUnitIds.ToHashSet();
            var historyMap = await _planExclusion.GetPlanHistoryMapAsync(Id, pageUnitIds);

            foreach (var item in pageItems)
            {
                item.AlreadyPlanned = plannedSet.Contains(item.UnitId);
                item.HasHistory = historyMap.TryGetValue(item.UnitId, out var hasHistory) && hasHistory;
            }

            Candidates = pageItems;
            PageUnitIds = pageUnitIds;

            AlreadyPlanned = await query.CountAsync(u => plannedUnitIds.Contains(u.Id));
            AvailableMatching = TotalMatching - AlreadyPlanned;
            WithHistory = Candidates.Count(c => c.AlreadyPlanned && c.HasHistory);
        }

        private IQueryable<EquipmentUnit> ApplyFilters(IQueryable<EquipmentUnit> query)
        {
            if (LaboratoryId.HasValue)
            {
                query = query.Where(u => u.LaboratoryId == LaboratoryId.Value);
            }

            if (Category.HasValue)
            {
                query = query.Where(u => u.Equipment!.Category == Category.Value);
            }

            if (TypeClassification.HasValue)
            {
                query = query.Where(u => u.Equipment!.TypeClassification == TypeClassification.Value);
            }

            if (UtensilType.HasValue)
            {
                query = query.Where(u => u.Equipment!.UtensilType == UtensilType.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.InventoryNumber.ToLower().Contains(term) ||
                    u.Equipment!.Name.ToLower().Contains(term) ||
                    (u.SerialNumber != null && u.SerialNumber.ToLower().Contains(term)));
            }

            return query;
        }

        private IActionResult RedirectToCurrentPage()
        {
            return RedirectToPage(new { id = Id, LaboratoryId, Category, TypeClassification, UtensilType, SearchTerm, PageIndex });
        }

        private IActionResult RedirectToManagementError(string message)
        {
            return RedirectToPage("/Error", new
            {
                module = "Gestion L-48",
                entityId = Id.ToString(),
                message,
                returnUrl = Url.Page("./PlanAssets", new { id = Id }),
                listUrl = Url.Page("./Index", new { Type = ManagementType.Preventive.ToString() })
            });
        }

        public class AssetCandidate
        {
            public int UnitId { get; set; }
            public string InventoryNumber { get; set; } = string.Empty;
            public string EquipmentName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public string LaboratoryName { get; set; } = string.Empty;
            public bool AlreadyPlanned { get; set; }
            public bool HasHistory { get; set; }
        }
    }
}
