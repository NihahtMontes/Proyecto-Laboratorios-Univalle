using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Paso 2: requerimientos por artículo de la planificación. Se precargan desde la receta de la práctica del
/// catálogo o el kit de la materia (cantidad escalada por estudiantes ÷ factor en las líneas escalables) y se
/// agregan con el buscador: artículos por categoría/nombre y equipos por nombre, inventario o serie (cada unidad
/// física es una línea). Los artículos "Por confirmar" se pueden pedir (D1) y se muestran con su etiqueta.
/// </summary>
public class RequerimientosModel : PageModel
{
    private static readonly string[] AdminRoles =
        AuthorizationHelper.AdminRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static readonly string[] ManagerRoles =
        AuthorizationHelper.ManagementRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly RequirementPickerService _picker;
    private readonly IArticleAvailabilityService _availability;
    private readonly IL3CircuitService _circuit;

    public RequerimientosModel(ApplicationDbContext context, IAcademicManagementService managements, RequirementPickerService picker,
        IArticleAvailabilityService availability, IL3CircuitService circuit)
    {
        _context = context;
        _managements = managements;
        _picker = picker;
        _availability = availability;
        _circuit = circuit;
    }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Conflict { get; set; }

    /// <summary>Pantalla de origen (p. ej. la grilla de conflictos): los POST y "Volver" regresan a ella.</summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>ReturnUrl validado (local) o null si no se indicó o no es seguro.</summary>
    public string? SafeReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

    /// <summary>Selección del buscador: artículos con cantidad y unidades físicas.</summary>
    [BindProperty]
    public BatchInput Batch { get; set; } = new();

    public Planning? Plan { get; private set; }
    public string Code => L3Codes.Format(PlanId);
    public bool IsEditable => Plan?.Status == PlanningStatus.Planned;
    public List<RequirementRow> Items { get; private set; } = [];

    /// <summary>Líneas de la receta de la práctica (para mostrar qué se precargará).</summary>
    public List<RecipeRow> Recipe { get; private set; } = [];
    public int? StudentCount { get; private set; }
    public int? ScalingFactor { get; private set; }

    /// <summary>Kit de la materia de la planificación (vacío si no tiene).</summary>
    public List<KitRow> Kit { get; private set; } = [];
    public string? SubjectName { get; private set; }
    /// <summary>Guardar las líneas como kit requiere los roles del catálogo de materias.</summary>
    public bool CanEditKit => User.IsInRole(AuthorizationHelper.RoleAdministrator) || User.IsInRole(AuthorizationHelper.RoleSuperAdmin);

    /// <summary>Datos del buscador: categorías de artículos y laboratorios de la sede/facultad (filtro de equipos).</summary>
    public List<RequirementPickerService.CategoryOption> Categories { get; private set; } = [];
    public List<LaboratoryOption> Laboratories { get; private set; } = [];

    public sealed record RequirementRow(int DetailId, int ArticleId, string ArticleCode, string ArticleName, bool ArticlePending,
        string? Unit, decimal? Quantity, string? InventoryNumber, string? Observations)
    {
        /// <summary>Stock del artículo (existencias en el Economato o unidades físicas del equipo vinculado).</summary>
        public ArticleStockInfo? Stock { get; init; }
    }

    /// <summary>Cantidad total pedida por artículo (varias líneas del mismo artículo comparten el stock).</summary>
    public Dictionary<int, decimal> RequestedByArticle { get; private set; } = [];

    /// <summary>True si el total pedido de este artículo supera su stock registrado.</summary>
    public bool IsShort(RequirementRow row) =>
        row.Stock?.Available is decimal available && RequestedByArticle.GetValueOrDefault(row.ArticleId) > available;

    /// <summary>Artículos de inventario sin stock (sin registro o en 0): bloquean "Validar disponibilidad".</summary>
    public List<PlanningStockGate.MissingStock> MissingStock { get; private set; } = [];

    /// <summary>Laboratorio donde se registra el stock faltante (Economato).</summary>
    public int? StockLaboratoryId { get; private set; }

    /// <summary>Equipo vinculado a cada artículo de la solicitud (null si es de inventario).</summary>
    public Dictionary<int, int?> EquipmentIdByArticle { get; private set; } = [];

    /// <summary>Roles que habilitan actualizar el stock de inventario (Administrador/SuperAdmin).</summary>
    public bool IsAdmin => AdminRoles.Any(User.IsInRole);

    /// <summary>Roles que habilitan registrar unidades físicas de equipo (Supervisor y superiores).</summary>
    public bool CanManage => ManagerRoles.Any(User.IsInRole);

    public bool IsMissingStock(RequirementRow row) => PlanningStockGate.IsMissing(row.Stock);

    /// <summary>Registrar el stock de un artículo y volver a estos requerimientos al guardar o cancelar.</summary>
    public string? StockUrl(int articleId) => StockLaboratoryId is int labId
        ? PlanningStockGate.StockEditUrl(labId, articleId, StockActionLinks.WithFocus(ReturnHereUrl, articleId))
        : null;

    /// <summary>Acción de stock para la fila (null si no hace falta o el usuario no tiene permiso).</summary>
    public StockAction? RowStockAction(RequirementRow item) =>
        StockActionLinks.For(item.Stock, RequestedByArticle.GetValueOrDefault(item.ArticleId), item.ArticleId,
            EquipmentIdByArticle.GetValueOrDefault(item.ArticleId), StockLaboratoryId,
            StockActionLinks.WithFocus(ReturnHereUrl, item.ArticleId), IsAdmin, CanManage);

    /// <summary>Recarga la página conservando la pantalla de origen (si la hay).</summary>
    private IActionResult Reload() => RedirectToPage(new { planId = PlanId, returnUrl = SafeReturnUrl });

    private string ReturnHereUrl => Url.Page("./Requerimientos", new { planId = PlanId, returnUrl = SafeReturnUrl }) ?? $"/LaboratoryOperations/Requerimientos?planId={PlanId}";
    public sealed record RecipeRow(string ArticleCode, string ArticleName, bool ArticlePending, decimal? BaseQuantity,
        bool IsScalable, decimal? ComputedQuantity, string? Note);
    public sealed record KitRow(string ArticleCode, string ArticleName, string? Category, bool IsEquipment, decimal BaseQuantity,
        bool IsScalable, decimal? ComputedQuantity, string? Note, bool AlreadyInRequest);
    public sealed record LaboratoryOption(int Id, string Name);

    /// <summary>Franja superior del wizard: gestión de la planificación y paso actual.</summary>
    public PlanningContextViewModel? Context { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Context = await PlanningContextLoader.ForPlanningAsync(_context, _managements, PlanId, 2);
        return await LoadPageAsync();
    }

    /// <summary>Precarga la receta: agrega las líneas que aún no están en la solicitud.</summary>
    public async Task<IActionResult> OnPostLoadRecipeAsync()
    {
        var plan = await GuardAsync();
        if (plan == null) return Reload();

        var schedule = plan.PracticeScheduleId.HasValue
            ? await _context.PracticeSchedules.AsNoTracking().FirstOrDefaultAsync(s => s.Id == plan.PracticeScheduleId.Value)
            : null;
        if (schedule == null)
        {
            TempData.Warning("La planificación no está vinculada a una práctica del catálogo: agrega los artículos manualmente.");
            return Reload();
        }

        var recipe = await _context.PracticeRequirements.AsNoTracking()
            .Include(r => r.Article)
            .Where(r => r.PracticeId == schedule.PracticeId && !r.IsRemoved && r.Article!.Status == GeneralStatus.Activo)
            .ToListAsync();
        if (recipe.Count == 0)
        {
            TempData.Warning("La práctica del catálogo no tiene receta registrada.");
            return Reload();
        }

        var present = await _context.PlanningRequirements.AsNoTracking()
            .Where(d => d.PlanningId == plan.Id).Select(d => d.ArticleId).ToListAsync();
        int added = 0, skipped = 0;
        var warnings = new List<string>();
        foreach (var line in recipe.Where(r => !present.Contains(r.ArticleId)))
        {
            var (quantity, note) = Scale(line, schedule.StudentCount, schedule.ScalingFactor);
            if (quantity == null)
            {
                skipped++;
                warnings.Add($"{line.Article!.Code}: {note}");
                continue;
            }
            _context.PlanningRequirements.Add(new PlanningRequirement
            {
                PlanningId = plan.Id,
                ArticleId = line.ArticleId,
                RequestedQuantity = quantity,
                ExpectedQuantity = (int)Math.Ceiling(quantity.Value),
                Observations = note
            });
            added++;
        }
        await _context.SaveChangesAsync();

        if (added > 0) TempData.Success($"Receta cargada: {added} artículo(s) agregados.");
        else TempData.Info("Todos los artículos de la receta ya estaban en la solicitud.");
        if (skipped > 0) TempData.Warning($"{skipped} línea(s) sin cantidad en la receta no se cargaron: {string.Join("; ", warnings.Take(5))}");
        return Reload();
    }

    /// <summary>Carga el kit de la materia: agrega los artículos que aún no están en la solicitud.</summary>
    public async Task<IActionResult> OnPostLoadKitAsync()
    {
        var plan = await GuardAsync();
        if (plan == null) return Reload();
        if (!plan.SubjectId.HasValue)
        {
            TempData.Warning("La planificación no tiene materia: agrega los artículos con el buscador.");
            return Reload();
        }

        var kit = await _context.SubjectKitItems.AsNoTracking()
            .Where(k => k.SubjectId == plan.SubjectId.Value && k.Article!.Status == GeneralStatus.Activo)
            .ToListAsync();
        if (kit.Count == 0)
        {
            TempData.Warning("Esta materia aún no tiene kit.");
            return Reload();
        }

        var schedule = plan.PracticeScheduleId.HasValue
            ? await _context.PracticeSchedules.AsNoTracking().FirstOrDefaultAsync(s => s.Id == plan.PracticeScheduleId.Value)
            : null;
        // Un artículo ya pedido (por cantidad o como unidades físicas) no se vuelve a cargar.
        var present = await _context.PlanningRequirements.AsNoTracking()
            .Where(d => d.PlanningId == plan.Id)
            .Select(d => d.ArticleId).ToListAsync();

        var added = 0;
        foreach (var item in kit.Where(k => !present.Contains(k.ArticleId)))
        {
            var (quantity, note) = RequirementPickerService.Scale(item.Quantity, item.IsScalable, schedule?.StudentCount, schedule?.ScalingFactor);
            var observations = string.Join(" · ", new[] { item.Observations, note }.Where(t => !string.IsNullOrWhiteSpace(t)));
            _context.PlanningRequirements.Add(new PlanningRequirement
            {
                PlanningId = plan.Id,
                ArticleId = item.ArticleId,
                RequestedQuantity = quantity,
                ExpectedQuantity = (int)Math.Ceiling(quantity ?? 0),
                Observations = observations.Length == 0 ? null : observations[..Math.Min(observations.Length, 1000)]
            });
            added++;
        }
        await _context.SaveChangesAsync();

        if (added > 0) TempData.Success($"Kit de la materia cargado: {added} artículo(s) agregados. Ajusta las cantidades si hace falta.");
        else TempData.Info("Todos los artículos del kit ya estaban en la solicitud.");
        return Reload();
    }

    /// <summary>Agrega lo elegido en el buscador: artículos (acumula cantidad) y unidades físicas (una línea por unidad).</summary>
    public async Task<IActionResult> OnPostAddBatchAsync()
    {
        var plan = await GuardAsync();
        if (plan == null) return Reload();

        var articleLines = Batch.Articles.Where(l => l.Id > 0)
            .GroupBy(l => l.Id)
            .Select(g => (ArticleId: g.Key, Quantity: g.Sum(l => l.Quantity ?? 0)))
            .ToList();
        var unitIds = Batch.UnitIds.Where(id => id > 0).Distinct().ToList();
        if (articleLines.Count == 0 && unitIds.Count == 0)
        {
            TempData.Warning("Selecciona al menos un artículo o una unidad de equipo.");
            return Reload();
        }
        if (articleLines.Any(l => l.Quantity <= 0 || l.Quantity > 999999 || decimal.Round(l.Quantity, 2) != l.Quantity))
        {
            TempData.Error("Las cantidades deben ser mayores a 0 y admiten hasta 2 decimales.");
            return Reload();
        }

        var articleIds = articleLines.Select(l => l.ArticleId).ToList();
        var activeArticles = await _context.Articles.AsNoTracking()
            .CountAsync(a => articleIds.Contains(a.Id) && a.Status == GeneralStatus.Activo);
        if (activeArticles != articleIds.Count)
        {
            TempData.Error("Algún artículo seleccionado ya no está activo. Vuelve a buscarlo.");
            return Reload();
        }

        var units = new List<(int Id, int EquipmentId)>();
        if (unitIds.Count > 0)
        {
            var scope = await ScopeAsync(plan.Id);
            if (scope == null)
            {
                TempData.Error("La planificación no tiene facultad para ubicar unidades de equipo.");
                return Reload();
            }
            var labIds = _picker.ScopeLaboratories(scope.Value.FacultyId, scope.Value.CampusId).Select(l => l.Id);
            units = (await _context.EquipmentUnits.AsNoTracking()
                    .Where(u => unitIds.Contains(u.Id) && u.LaboratoryId != null && labIds.Contains(u.LaboratoryId.Value)
                        && u.CurrentStatus != EquipmentStatus.Deleted && u.CurrentStatus != EquipmentStatus.Decommissioned
                        && u.CurrentStatus != EquipmentStatus.Dismantled
                        && u.Equipment != null && u.Equipment.Status == GeneralStatus.Activo)
                    .Select(u => new { u.Id, u.EquipmentId })
                    .ToListAsync())
                .Select(u => (u.Id, u.EquipmentId))
                .ToList();
            if (units.Count != unitIds.Count)
            {
                TempData.Error("Alguna unidad seleccionada no está disponible en la sede y facultad de la planificación.");
                return Reload();
            }
        }

        int added = 0, merged = 0, skipped = 0;
        var unitArticleIds = new List<int>();
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Primero se resuelven (o crean) los artículos de los equipos; ResolveEquipmentArticleAsync guarda.
            var articleByEquipment = new Dictionary<int, int>();
            foreach (var equipmentId in units.Select(u => u.EquipmentId).Distinct())
                articleByEquipment[equipmentId] = await _picker.ResolveEquipmentArticleAsync(equipmentId);

            var existing = await _context.PlanningRequirements.AsTracking()
                .Where(d => d.PlanningId == plan.Id)
                .ToListAsync();

            foreach (var unit in units)
            {
                if (existing.Any(d => d.EquipmentUnitId == unit.Id))
                {
                    skipped++;
                    continue;
                }
                var detail = new PlanningRequirement
                {
                    PlanningId = plan.Id,
                    ArticleId = articleByEquipment[unit.EquipmentId],
                    EquipmentUnitId = unit.Id,
                    RequestedQuantity = 1,
                    ExpectedQuantity = 1
                };
                _context.PlanningRequirements.Add(detail);
                existing.Add(detail);
                unitArticleIds.Add(articleByEquipment[unit.EquipmentId]);
                added++;
            }

            foreach (var line in articleLines)
            {
                var current = existing.FirstOrDefault(d => d.ArticleId == line.ArticleId && d.EquipmentUnitId == null);
                if (current != null)
                {
                    current.RequestedQuantity = (current.RequestedQuantity ?? 0) + line.Quantity;
                    current.ExpectedQuantity = (int)Math.Ceiling(current.RequestedQuantity.Value);
                    merged++;
                }
                else
                {
                    var detail = new PlanningRequirement
                    {
                        PlanningId = plan.Id,
                        ArticleId = line.ArticleId,
                        RequestedQuantity = line.Quantity,
                        ExpectedQuantity = (int)Math.Ceiling(line.Quantity)
                    };
                    _context.PlanningRequirements.Add(detail);
                    existing.Add(detail);
                    added++;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData.Error("No se pudieron agregar los requerimientos. Intente nuevamente.");
            return Reload();
        }

        var parts = new List<string>();
        if (added > 0) parts.Add($"{added} línea(s) agregada(s)");
        if (merged > 0) parts.Add($"{merged} cantidad(es) sumada(s) a líneas existentes");
        if (parts.Count > 0) TempData.Success($"Requerimientos actualizados: {string.Join(" y ", parts)}.");
        if (skipped > 0) TempData.Info($"{skipped} unidad(es) ya estaban en la solicitud.");
        await PromptStockAsync(articleLines.Select(l => l.ArticleId).Concat(unitArticleIds).Distinct().ToList());
        return Reload();
    }

    /// <summary>Artículo ofrecido para registro inmediato desde el aviso "¿Registrarlo ahora?".</summary>
    public sealed record StockPromptItem(string Name, string Url, string Label);

    /// <summary>
    /// Tras agregar requerimientos: si algún artículo quedó sin stock suficiente, guarda en TempData
    /// (StockPrompt) la lista para ofrecer registrar su existencia al volver; máximo 5 artículos.
    /// </summary>
    private async Task PromptStockAsync(List<int> articleIds)
    {
        if (articleIds.Count == 0) return;
        var requested = await _context.PlanningRequirements.AsNoTracking()
            .Where(d => d.PlanningId == PlanId && articleIds.Contains(d.ArticleId))
            .GroupBy(d => d.ArticleId)
            .Select(g => new { ArticleId = g.Key, Total = g.Sum(d => d.RequestedQuantity ?? d.ExpectedQuantity) })
            .ToDictionaryAsync(x => x.ArticleId, x => x.Total);
        if (requested.Count == 0) return;

        var ids = requested.Keys.ToList();
        var stock = await _availability.GetStockAsync(ids, null);
        var articles = await _context.Articles.AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .Select(a => new { a.Id, a.Name, a.EquipmentId })
            .ToDictionaryAsync(a => a.Id, a => (Name: a.Name, EquipmentId: a.EquipmentId));

        int? laboratoryId = null;
        var items = new List<StockPromptItem>();
        foreach (var id in ids)
        {
            var info = stock.GetValueOrDefault(id);
            var total = requested.GetValueOrDefault(id);
            if (!StockActionLinks.NeedsAction(info, total)) continue;
            var (name, equipmentId) = articles.GetValueOrDefault(id);
            if (info?.Source != StockSource.EquipmentUnits && !equipmentId.HasValue && laboratoryId == null)
                laboratoryId = await PlanningStockGate.StockLaboratoryIdAsync(_context, PlanId);
            var action = StockActionLinks.For(info, total, id, equipmentId, laboratoryId,
                StockActionLinks.WithFocus(ReturnHereUrl, id), IsAdmin, CanManage);
            if (action != null)
                items.Add(new StockPromptItem(name, action.Url, action.Label));
        }
        if (items.Count == 0) return;

        TempData["StockPrompt"] = System.Text.Json.JsonSerializer.Serialize(items.Take(5),
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
    }

    /// <summary>
    /// Reemplaza el kit de la materia con las líneas actuales (sumadas por artículo). Las líneas escalables del kit
    /// conservan su cantidad base, porque la de la práctica ya viene escalada.
    /// </summary>
    public async Task<IActionResult> OnPostSaveKitAsync()
    {
        if (!CanEditKit)
        {
            TempData.Error("No tiene permisos para modificar el kit de la materia.");
            return Reload();
        }
        var plan = await _context.Plannings.AsNoTracking().FirstOrDefaultAsync(r => r.Id == PlanId);
        if (plan?.SubjectId is not int subjectId)
        {
            TempData.Error("La planificación no tiene materia.");
            return Reload();
        }

        var lines = (await _context.PlanningRequirements.AsNoTracking()
                .Where(d => d.PlanningId == plan.Id)
                .Select(d => new { d.ArticleId, Quantity = d.RequestedQuantity ?? d.ExpectedQuantity })
                .ToListAsync())
            .GroupBy(d => d.ArticleId)
            .Select(g => (ArticleId: g.Key, Quantity: g.Sum(d => d.Quantity)))
            .Where(l => l.Quantity > 0)
            .ToList();
        if (lines.Count == 0)
        {
            TempData.Error("La solicitud no tiene líneas con cantidad para guardar como kit.");
            return Reload();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var kit = await _context.SubjectKitItems.AsTracking()
            .Where(k => k.SubjectId == subjectId)
            .ToListAsync();
        foreach (var line in lines)
        {
            var item = kit.FirstOrDefault(k => k.ArticleId == line.ArticleId);
            if (item == null)
                _context.SubjectKitItems.Add(new SubjectKitItem { SubjectId = subjectId, ArticleId = line.ArticleId, Quantity = line.Quantity });
            else if (!item.IsScalable)
                item.Quantity = line.Quantity;
        }
        var keep = lines.Select(l => l.ArticleId).ToHashSet();
        foreach (var item in kit.Where(k => !keep.Contains(k.ArticleId)))
            item.IsRemoved = true;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData.Success($"Kit de la materia guardado con {lines.Count} artículo(s).");
        return Reload();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int detailId, decimal newQuantity)
    {
        var plan = await GuardAsync();
        if (plan == null) return Reload();

        if (newQuantity <= 0 || newQuantity > 999999 || decimal.Round(newQuantity, 2) != newQuantity)
        {
            TempData.Error("La cantidad debe ser mayor a 0 y admite hasta 2 decimales.");
            return Reload();
        }

        var detail = await _context.PlanningRequirements.AsTracking()
            .FirstOrDefaultAsync(d => d.Id == detailId && d.PlanningId == plan.Id);
        if (detail == null)
        {
            TempData.Error("El requerimiento no pertenece a esta planificación.");
            return Reload();
        }
        if (detail.EquipmentUnitId.HasValue && newQuantity != 1)
        {
            TempData.Error("Una unidad física de equipo siempre se solicita con cantidad 1.");
            return Reload();
        }

        detail.RequestedQuantity = newQuantity;
        detail.ExpectedQuantity = (int)Math.Ceiling(newQuantity);
        await _context.SaveChangesAsync();
        TempData.Success("Cantidad actualizada.");
        return Reload();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int detailId)
    {
        var plan = await GuardAsync();
        if (plan == null) return Reload();

        var detail = await _context.PlanningRequirements.AsTracking()
            .FirstOrDefaultAsync(d => d.Id == detailId && d.PlanningId == plan.Id);
        if (detail != null)
        {
            // Línea de borrador de una planificación aún no aprobada: no tiene salidas ni historial que preservar.
            _context.PlanningRequirements.Remove(detail);
            await _context.SaveChangesAsync();
            TempData.Success("Requerimiento retirado.");
        }
        return Reload();
    }

    public async Task<IActionResult> OnPostContinueAsync()
    {
        var plan = await GuardAsync();
        if (plan == null) return Reload();

        if (!await _context.PlanningRequirements.AsNoTracking().AnyAsync(d => d.PlanningId == plan.Id))
        {
            TempData.Error("Agrega al menos un requerimiento antes de validar disponibilidad.");
            return Reload();
        }

        // No se planifica lo que no tiene stock: se lleva al usuario a registrar la existencia del primero
        // y la pantalla de existencias lo devuelve aquí al guardar o cancelar.
        var missing = await PlanningStockGate.FindAsync(_context, _availability, plan.Id);
        if (missing.Count > 0)
        {
            StockLaboratoryId = await PlanningStockGate.StockLaboratoryIdAsync(_context, plan.Id);
            var names = string.Join(", ", missing.Take(5).Select(m => m.ArticleName)) + (missing.Count > 5 ? "…" : "");
            TempData.Warning($"Registre el stock antes de continuar. Sin stock: {names}.");
            return StockUrl(missing[0].ArticleId) is string url
                ? LocalRedirect(url)
                : Reload();
        }

        return RedirectToPage("./Disponibilidad", new { planId = PlanId });
    }

    /// <summary>Cantidad de una línea de receta: base, o base × estudiantes ÷ factor (redondeada hacia arriba) si es escalable.</summary>
    public static (decimal? Quantity, string? Note) Scale(PracticeRequirement line, int? students, int? factor) =>
        line.Quantity == null
            ? (null, "la receta no trae cantidad")
            : RequirementPickerService.Scale(line.Quantity, line.IsScalable, students, factor);

    private Task<(int FacultyId, int? CampusId)?> ScopeAsync(int practiceId) => _picker.PracticeScopeAsync(practiceId);

    private async Task<Planning?> GuardAsync()
    {
        if (!await PlanningContextLoader.IsWritableAsync(_managements, PlanId))
        {
            TempData.Error(PlanningContextLoader.ReadOnlyMessage);
            return null;
        }
        var plan = await _context.Plannings.AsNoTracking().FirstOrDefaultAsync(r => r.Id == PlanId);
        if (plan == null)
        {
            TempData.Error("La planificación no existe.");
            return null;
        }
        if (plan.Status != PlanningStatus.Planned)
        {
            TempData.Error("La planificación ya fue aprobada; sus requerimientos no se pueden modificar.");
            return null;
        }
        return plan;
    }

    private async Task<IActionResult> LoadPageAsync()
    {
        Plan = await _context.Plannings.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.TeacherPerson)
            .Include(r => r.PracticeSchedule)
            .FirstOrDefaultAsync(r => r.Id == PlanId);
        if (Plan == null)
        {
            TempData.Error("La planificación no existe.");
            return RedirectToPage("./Index");
        }

        StudentCount = Plan.PracticeSchedule?.StudentCount;
        ScalingFactor = Plan.PracticeSchedule?.ScalingFactor;

        Items = await _context.PlanningRequirements.AsNoTracking()
            .Where(d => d.PlanningId == Plan.Id)
            .OrderBy(d => d.Article!.Name)
            .ThenBy(d => d.EquipmentUnit != null ? d.EquipmentUnit.InventoryNumber : null)
            .Select(d => new RequirementRow(d.Id, d.ArticleId, d.Article!.Code, d.Article.Name,
                d.Article.ReviewStatus == DataReviewStatus.PorConfirmar,
                d.Article.MeasurementUnit != null ? d.Article.MeasurementUnit.Name : d.Article.UnitOfMeasure,
                d.RequestedQuantity,
                d.EquipmentUnit != null
                    ? d.EquipmentUnit.InventoryNumber + (d.EquipmentUnit.SerialNumber != null && d.EquipmentUnit.SerialNumber != "" ? " · S/N " + d.EquipmentUnit.SerialNumber : "")
                    : null,
                d.Observations))
            .ToListAsync();
        var stock = await _availability.GetStockAsync(Items.Select(i => i.ArticleId), null); // total institucional (D12)
        Items = Items.Select(i => i with { Stock = stock.GetValueOrDefault(i.ArticleId) }).ToList();
        RequestedByArticle = Items.GroupBy(i => i.ArticleId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity ?? 0));
        EquipmentIdByArticle = await _context.Articles.AsNoTracking()
            .Where(a => Items.Select(i => i.ArticleId).Contains(a.Id))
            .Select(a => new { a.Id, a.EquipmentId })
            .ToDictionaryAsync(a => a.Id, a => a.EquipmentId);
        MissingStock = Items
            .Where(IsMissingStock)
            .GroupBy(i => i.ArticleId)
            .Select(g => new PlanningStockGate.MissingStock(g.Key, g.First().ArticleCode, g.First().ArticleName))
            .OrderBy(m => m.ArticleName)
            .ToList();
        // El laboratorio de stock se necesita si alguna fila ofrece acción de inventario (no solo faltantes).
        if (Items.Any(i => StockActionLinks.NeedsAction(i.Stock, RequestedByArticle.GetValueOrDefault(i.ArticleId))))
            StockLaboratoryId = await PlanningStockGate.StockLaboratoryIdAsync(_context, Plan.Id);

        if (IsEditable)
        {
            Categories = await _picker.CategoriesAsync();
            var scope = await ScopeAsync(Plan.Id);
            if (scope != null)
                Laboratories = await _picker.ScopeLaboratories(scope.Value.FacultyId, scope.Value.CampusId)
                    .OrderBy(l => l.Code)
                    .Select(l => new LaboratoryOption(l.Id, l.Code + " · " + l.Name))
                    .ToListAsync();
        }

        SubjectName = Plan.Subject?.Name;
        if (Plan.SubjectId.HasValue)
        {
            var present = Items.Select(i => i.ArticleId).ToHashSet();
            var kit = await _context.SubjectKitItems.AsNoTracking()
                .Where(k => k.SubjectId == Plan.SubjectId.Value)
                .OrderBy(k => k.Article!.Name)
                .Select(k => new
                {
                    k.ArticleId, k.Article!.Code, k.Article.Name, k.Quantity, k.IsScalable, k.Observations,
                    Category = k.Article.ArticleCategory != null ? k.Article.ArticleCategory.Name : k.Article.Category,
                    IsEquipment = k.Article.EquipmentId != null
                })
                .ToListAsync();
            Kit = kit.Select(k =>
            {
                var (quantity, note) = RequirementPickerService.Scale(k.Quantity, k.IsScalable, StudentCount, ScalingFactor);
                return new KitRow(k.Code, k.Name, k.Category, k.IsEquipment, k.Quantity, k.IsScalable, quantity,
                    note ?? k.Observations, present.Contains(k.ArticleId));
            }).ToList();
        }

        if (Plan.PracticeSchedule != null)
        {
            var recipe = await _context.PracticeRequirements.AsNoTracking()
                .Include(r => r.Article)
                .Where(r => r.PracticeId == Plan.PracticeSchedule.PracticeId && !r.IsRemoved)
                .OrderBy(r => r.Article!.Name)
                .ToListAsync();
            Recipe = recipe.Select(r =>
            {
                var (quantity, note) = Scale(r, StudentCount, ScalingFactor);
                return new RecipeRow(r.Article!.Code, r.Article.Name, r.Article.ReviewStatus == DataReviewStatus.PorConfirmar,
                    r.Quantity, r.IsScalable, quantity, note);
            }).ToList();
        }

        return Page();
    }

    public sealed class BatchInput
    {
        public List<BatchLine> Articles { get; set; } = [];
        public List<int> UnitIds { get; set; } = [];
    }

    public sealed class BatchLine
    {
        public int Id { get; set; }
        public decimal? Quantity { get; set; }
    }
}
