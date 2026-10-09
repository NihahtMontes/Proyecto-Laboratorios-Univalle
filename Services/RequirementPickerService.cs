using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>
/// Buscador de requerimientos (artículos por categoría/nombre y equipos por nombre, inventario o serie), fichas de
/// detalle y resolución del artículo vinculado a un equipo del catálogo. Lo usan Requerimientos (paso 2) y el kit de la materia.
/// </summary>
public sealed class RequirementPickerService
{
    public const int PageSize = 50;
    private const string EquipmentCategoryName = "Equipo";
    private static readonly EquipmentStatus[] RetiredStatuses = [EquipmentStatus.Deleted, EquipmentStatus.Decommissioned, EquipmentStatus.Dismantled];

    private readonly ApplicationDbContext _context;
    private readonly IArticleAvailabilityService _availability;

    public RequirementPickerService(ApplicationDbContext context, IArticleAvailabilityService availability)
    {
        _context = context;
        _availability = availability;
    }

    public sealed record CategoryOption(int Id, string Name, int ArticleCount);
    public sealed record ArticleResult(int Id, string Code, string Name, string? Category, string? Unit, bool Pending,
        bool EquipmentLinked, decimal? Stock);
    public sealed record UnitResult(int Id, string InventoryNumber, string? SerialNumber, string? Laboratory, string Status, bool Available);
    public sealed record EquipmentGroup(int EquipmentId, string? CatalogCode, string Name, string? Brand, string? Model, List<UnitResult> Units);
    public sealed record EquipmentCatalogResult(int EquipmentId, string? CatalogCode, string Name, string? Brand, string? Model, int UnitCount);
    public sealed record SearchPage<T>(List<T> Items, bool Truncated, int Total);

    public sealed record StockRow(string Laboratory, decimal Available);
    public sealed record ArticleDetail(int Id, string Code, string Name, string? Category, string? Unit, bool Pending,
        int? EquipmentId, string? EquipmentName, List<StockRow> Stocks, int RecipeCount, int KitCount, string EditUrl, string? EquipmentUrl);
    public sealed record UnitDetail(int Id, string InventoryNumber, string? SerialNumber, string Status, string? Condition,
        string? Laboratory, string? InternalLocation, DateTime? AcquisitionDate, string? Notes,
        int EquipmentId, string EquipmentName, string? CatalogCode, string? Brand, string? Model, string? Description,
        string DetailsUrl, string EquipmentUrl);
    public sealed record EquipmentDetail(int Id, string Name, string? CatalogCode, string? Brand, string? Model, string? Description,
        string Category, int UnitCount, List<UnitResult> Units, string EquipmentUrl);

    public Task<List<CategoryOption>> CategoriesAsync() =>
        _context.ArticleCategories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption(c.Id, c.Name,
                _context.Articles.Count(a => a.ArticleCategoryId == c.Id && a.Status == GeneralStatus.Activo)))
            .ToListAsync();

    /// <summary>Artículos activos por categoría y texto; <paramref name="confirmedOnly"/> oculta los "Por confirmar".</summary>
    public async Task<SearchPage<ArticleResult>> SearchArticlesAsync(int? categoryId, string? term, bool confirmedOnly = false)
    {
        var query = _context.Articles.AsNoTracking().Where(a => a.Status == GeneralStatus.Activo);
        if (categoryId is > 0) query = query.Where(a => a.ArticleCategoryId == categoryId);
        if (confirmedOnly) query = query.Where(a => a.ReviewStatus != DataReviewStatus.PorConfirmar);
        foreach (var word in Words(term))
            query = query.Where(a => a.Name.Contains(word) || a.Code.Contains(word));

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(a => a.Name)
            .Take(PageSize)
            .Select(a => new ArticleResult(a.Id, a.Code, a.Name,
                a.ArticleCategory != null ? a.ArticleCategory.Name : a.Category,
                a.MeasurementUnit != null ? a.MeasurementUnit.Name : a.UnitOfMeasure,
                a.ReviewStatus == DataReviewStatus.PorConfirmar,
                a.EquipmentId != null,
                _context.ArticleStocks.Any(s => s.ArticleId == a.Id)
                    ? _context.ArticleStocks.Where(s => s.ArticleId == a.Id).Sum(s => s.AvailableQuantity)
                    : (decimal?)null))
            .ToListAsync();

        // Equipos vinculados: el stock son sus unidades físicas disponibles, no las existencias.
        var linked = rows.Where(r => r.EquipmentLinked).Select(r => r.Id).ToList();
        if (linked.Count > 0)
        {
            var units = await _availability.GetStockAsync(linked, null);
            rows = rows.Select(r => r.EquipmentLinked && units.TryGetValue(r.Id, out var info) ? r with { Stock = info.Available } : r).ToList();
        }
        return new SearchPage<ArticleResult>(rows, total > PageSize, total);
    }

    /// <summary>Facultad y sede de una planificación: de la carrera de su materia y, si no tiene, del laboratorio.</summary>
    public async Task<(int FacultyId, int? CampusId)?> PracticeScopeAsync(int practiceId)
    {
        var row = await _context.Plannings.AsNoTracking()
            .Where(r => r.Id == practiceId)
            .Select(r => new
            {
                SubjectFaculty = r.Subject != null && r.Subject.Career != null ? r.Subject.Career.FacultadId : null,
                PlanningCampus = r.CampusId,
                LabFaculty = r.Laboratory != null ? (int?)r.Laboratory.FacultyId : null,
                LabCampus = r.Laboratory != null ? r.Laboratory.CampusId : null
            })
            .FirstOrDefaultAsync();
        if (row == null) return null;
        if (row.SubjectFaculty is int faculty) return (faculty, row.PlanningCampus ?? row.LabCampus);
        return row.LabFaculty is int labFaculty ? (labFaculty, row.LabCampus) : null;
    }

    /// <summary>Laboratorios donde se buscan unidades físicas: los de la facultad (y sede, si la hay).</summary>
    public IQueryable<Laboratory> ScopeLaboratories(int facultyId, int? campusId) =>
        _context.Laboratories.AsNoTracking()
            .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo && (campusId == null || l.CampusId == campusId));

    /// <summary>
    /// Unidades físicas por nombre del equipo, código de catálogo, n.º de inventario o n.º de serie, agrupadas por equipo.
    /// Filtros: laboratorio, solo operativas, solo con n.º de serie.
    /// </summary>
    public async Task<SearchPage<EquipmentGroup>> SearchUnitsAsync(int facultyId, int? campusId, int? laboratoryId, string? term,
        bool operationalOnly = false, bool withSerialOnly = false)
    {
        var labIds = ScopeLaboratories(facultyId, campusId).Select(l => l.Id);
        var query = _context.EquipmentUnits.AsNoTracking()
            .Where(u => u.LaboratoryId != null && labIds.Contains(u.LaboratoryId.Value)
                && !RetiredStatuses.Contains(u.CurrentStatus)
                && u.Equipment != null && u.Equipment.Status == GeneralStatus.Activo);
        if (laboratoryId is > 0) query = query.Where(u => u.LaboratoryId == laboratoryId);
        if (operationalOnly) query = query.Where(u => u.CurrentStatus == EquipmentStatus.Operational);
        if (withSerialOnly) query = query.Where(u => u.SerialNumber != null && u.SerialNumber != "");
        foreach (var word in Words(term))
            query = query.Where(u => u.Equipment!.Name.Contains(word)
                || (u.Equipment.CatalogCode != null && u.Equipment.CatalogCode.Contains(word))
                || u.InventoryNumber.Contains(word)
                || (u.SerialNumber != null && u.SerialNumber.Contains(word)));

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(u => u.Equipment!.Name)
            .ThenBy(u => u.SerialNumber == null || u.SerialNumber == "")
            .ThenBy(u => u.SerialNumber)
            .ThenBy(u => u.InventoryNumber)
            .Take(PageSize)
            .Select(u => new
            {
                u.Id, u.EquipmentId, u.InventoryNumber, u.SerialNumber, u.CurrentStatus,
                Laboratory = u.Laboratory != null ? u.Laboratory.Code + " · " + u.Laboratory.Name : null,
                u.Equipment!.CatalogCode, EquipmentName = u.Equipment.Name, u.Equipment.Brand, u.Equipment.Model
            })
            .ToListAsync();

        var groups = rows
            .GroupBy(r => r.EquipmentId)
            .Select(g =>
            {
                var first = g.First();
                return new EquipmentGroup(g.Key, first.CatalogCode, first.EquipmentName, first.Brand, first.Model,
                    g.Select(u => new UnitResult(u.Id, u.InventoryNumber, string.IsNullOrWhiteSpace(u.SerialNumber) ? null : u.SerialNumber,
                        u.Laboratory, EnumHelper.GetDisplayName(u.CurrentStatus), u.CurrentStatus == EquipmentStatus.Operational)).ToList());
            })
            .ToList();
        return new SearchPage<EquipmentGroup>(groups, total > PageSize, total);
    }

    /// <summary>Equipos del catálogo (sin elegir unidad), para el kit de la materia.</summary>
    public async Task<SearchPage<EquipmentCatalogResult>> SearchEquipmentCatalogAsync(string? term, bool withUnitsOnly = false)
    {
        var query = _context.Equipments.AsNoTracking().Where(e => e.Status == GeneralStatus.Activo);
        if (withUnitsOnly)
            query = query.Where(e => _context.EquipmentUnits.Any(u => u.EquipmentId == e.Id && !RetiredStatuses.Contains(u.CurrentStatus)));
        foreach (var word in Words(term))
            query = query.Where(e => e.Name.Contains(word) || (e.CatalogCode != null && e.CatalogCode.Contains(word))
                || (e.Brand != null && e.Brand.Contains(word)) || (e.Model != null && e.Model.Contains(word)));

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(e => e.Name)
            .Take(PageSize)
            .Select(e => new EquipmentCatalogResult(e.Id, e.CatalogCode, e.Name, e.Brand, e.Model,
                _context.EquipmentUnits.Count(u => u.EquipmentId == e.Id && !RetiredStatuses.Contains(u.CurrentStatus))))
            .ToListAsync();
        return new SearchPage<EquipmentCatalogResult>(rows, total > PageSize, total);
    }

    public async Task<ArticleDetail?> ArticleDetailAsync(int id)
    {
        var article = await _context.Articles.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id, a.Code, a.Name, a.EquipmentId,
                Category = a.ArticleCategory != null ? a.ArticleCategory.Name : a.Category,
                Unit = a.MeasurementUnit != null ? a.MeasurementUnit.Name : a.UnitOfMeasure,
                Pending = a.ReviewStatus == DataReviewStatus.PorConfirmar,
                EquipmentName = a.Equipment != null ? a.Equipment.Name : null
            })
            .FirstOrDefaultAsync();
        if (article == null) return null;

        var stocks = await _context.ArticleStocks.AsNoTracking()
            .Where(s => s.ArticleId == id)
            .OrderByDescending(s => s.AvailableQuantity)
            .Select(s => new StockRow(s.Laboratory != null ? s.Laboratory.Code + " · " + s.Laboratory.Name : "—", s.AvailableQuantity))
            .ToListAsync();
        var recipes = await _context.PracticeRequirements.AsNoTracking().CountAsync(r => r.ArticleId == id);
        var kits = await _context.SubjectKitItems.AsNoTracking().CountAsync(k => k.ArticleId == id);
        return new ArticleDetail(article.Id, article.Code, article.Name, article.Category, article.Unit, article.Pending,
            article.EquipmentId, article.EquipmentName, stocks, recipes, kits,
            $"/Catalogs/Articles/Edit/{article.Id}",
            article.EquipmentId is int equipmentId ? $"/Equipment/Details/{equipmentId}" : null);
    }

    public async Task<UnitDetail?> UnitDetailAsync(int id)
    {
        var unit = await _context.EquipmentUnits.AsNoTracking()
            .Where(u => u.Id == id && u.Equipment != null)
            .Select(u => new
            {
                u.Id, u.InventoryNumber, u.SerialNumber, u.CurrentStatus, u.PhysicalCondition, u.InternalLocation,
                u.AcquisitionDate, u.Notes, u.EquipmentId,
                Laboratory = u.Laboratory != null ? u.Laboratory.Code + " · " + u.Laboratory.Name : null,
                u.Equipment!.Name, u.Equipment.CatalogCode, u.Equipment.Brand, u.Equipment.Model, u.Equipment.Description
            })
            .FirstOrDefaultAsync();
        if (unit == null) return null;
        return new UnitDetail(unit.Id, unit.InventoryNumber, string.IsNullOrWhiteSpace(unit.SerialNumber) ? null : unit.SerialNumber,
            EnumHelper.GetDisplayName(unit.CurrentStatus),
            unit.PhysicalCondition is { } condition ? EnumHelper.GetDisplayName(condition) : null,
            unit.Laboratory, unit.InternalLocation, unit.AcquisitionDate, unit.Notes,
            unit.EquipmentId, unit.Name, unit.CatalogCode, unit.Brand, unit.Model, unit.Description,
            $"/EquipmentUnits/Details?id={unit.Id}", $"/Equipment/Details/{unit.EquipmentId}");
    }

    public async Task<EquipmentDetail?> EquipmentDetailAsync(int id)
    {
        var equipment = await _context.Equipments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (equipment == null) return null;

        var units = await _context.EquipmentUnits.AsNoTracking()
            .Where(u => u.EquipmentId == id && !RetiredStatuses.Contains(u.CurrentStatus))
            .OrderBy(u => u.SerialNumber == null || u.SerialNumber == "").ThenBy(u => u.SerialNumber).ThenBy(u => u.InventoryNumber)
            .Select(u => new
            {
                u.Id, u.InventoryNumber, u.SerialNumber, u.CurrentStatus,
                Laboratory = u.Laboratory != null ? u.Laboratory.Code + " · " + u.Laboratory.Name : null
            })
            .ToListAsync();
        return new EquipmentDetail(equipment.Id, equipment.Name, equipment.CatalogCode, equipment.Brand, equipment.Model,
            equipment.Description, EnumHelper.GetDisplayName(equipment.Category), units.Count,
            units.Take(PageSize).Select(u => new UnitResult(u.Id, u.InventoryNumber,
                string.IsNullOrWhiteSpace(u.SerialNumber) ? null : u.SerialNumber, u.Laboratory,
                EnumHelper.GetDisplayName(u.CurrentStatus), u.CurrentStatus == EquipmentStatus.Operational)).ToList(),
            $"/Equipment/Details/{equipment.Id}");
    }

    /// <summary>
    /// Artículo que representa a un equipo del catálogo (las líneas L-3 y el kit exigen artículo).
    /// Orden: artículo ya vinculado → artículo activo con el mismo nombre (se vincula) → artículo nuevo de categoría Equipo.
    /// Guarda en el contexto; el llamador lo invoca dentro de su transacción.
    /// </summary>
    public async Task<int> ResolveEquipmentArticleAsync(int equipmentId)
    {
        var linked = await _context.Articles.AsNoTracking()
            .Where(a => a.EquipmentId == equipmentId && a.Status == GeneralStatus.Activo)
            .OrderBy(a => a.Id)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();
        if (linked.HasValue) return linked.Value;

        var equipment = await _context.Equipments.AsNoTracking().FirstAsync(e => e.Id == equipmentId);
        var name = equipment.Name.Trim();

        var sameName = await _context.Articles.AsTracking()
            .FirstOrDefaultAsync(a => a.Name == name && a.Status == GeneralStatus.Activo && a.EquipmentId == null);
        if (sameName != null)
        {
            sameName.EquipmentId = equipmentId;
            await _context.SaveChangesAsync();
            return sameName.Id;
        }

        var categoryId = await _context.ArticleCategories.AsNoTracking()
            .Where(c => c.Name == EquipmentCategoryName).Select(c => (int?)c.Id).FirstOrDefaultAsync();
        var article = new Article
        {
            Code = string.Empty,
            Name = name.Length <= 200 ? name : name[..200],
            Category = EquipmentCategoryName,
            ArticleCategoryId = categoryId,
            UnitOfMeasure = "UNIDAD",
            EquipmentId = equipmentId,
            Status = GeneralStatus.Activo
        };
        _context.Articles.Add(article);
        await _context.SaveWithCodeAsync(
            _context.Articles.IgnoreQueryFilters().Select(a => (string?)a.Code),
            CatalogCodes.Article,
            code => article.Code = code);
        return article.Id;
    }

    /// <summary>Cantidad escalada: base, o base × estudiantes ÷ factor (hacia arriba) si es escalable.</summary>
    public static (decimal? Quantity, string? Note) Scale(decimal? baseQuantity, bool isScalable, int? students, int? factor)
    {
        if (baseQuantity is not decimal q) return (null, "sin cantidad registrada");
        if (!isScalable) return (q, null);
        if (students is not int s || s <= 0 || factor is not int f || f <= 0)
            return (q, "Escalable sin estudiantes o factor en la programación: se usa la cantidad base.");
        return (Math.Ceiling(q * s / f), $"Escalada: {q:0.##} × {s} estudiantes ÷ {f}.");
    }

    private static IEnumerable<string> Words(string? term) =>
        (term ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 0).Take(5);
}
