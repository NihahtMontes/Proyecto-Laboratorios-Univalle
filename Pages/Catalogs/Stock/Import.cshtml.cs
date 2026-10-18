using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Stock;

/// <summary>
/// Carga masiva de existencias desde Excel (mismo formato que la hoja ExistenciasArticulos):
/// LaboratorioCodigo · ArticuloCodigo · CantidadDisponible. Primero se valida y se muestra una vista previa;
/// al confirmar se crean las filas nuevas y se actualizan las cantidades distintas, en lotes de 100.
/// </summary>
[Authorize(Roles = AuthorizationHelper.AdminRoles)]
public class ImportModel : PageModel
{
    public const int MaxRows = 5000;
    private const int BatchSize = 100;
    private const string SessionPrefix = "StockImport:";
    private static readonly string[] Headers = ["LaboratorioCodigo", "ArticuloCodigo", "CantidadDisponible"];

    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImportModel> _logger;

    public ImportModel(ApplicationDbContext context, ILogger<ImportModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    /// <summary>Clave de la vista previa guardada en sesión (se confirma con ella).</summary>
    [BindProperty]
    public string? Token { get; set; }

    public string? FileName { get; private set; }
    public List<PreviewRow> Rows { get; private set; } = [];
    public bool HasPreview => Rows.Count > 0;
    public int NewCount => Rows.Count(r => r.Action == RowAction.Create);
    public int UpdateCount => Rows.Count(r => r.Action == RowAction.Update);
    public int UnchangedCount => Rows.Count(r => r.Action == RowAction.Unchanged);
    public int ErrorCount => Rows.Count(r => r.Action == RowAction.Error);
    public bool CanApply => NewCount + UpdateCount > 0;

    public enum RowAction { Create, Update, Unchanged, Error }

    public sealed record PreviewRow(
        int RowNumber, string LaboratoryCode, string ArticleCode, string? ArticleName, decimal? Quantity,
        decimal? CurrentQuantity, RowAction Action, string? Error, int LaboratoryId = 0, int ArticleId = 0);

    public void OnGet() { }

    /// <summary>Descarga las existencias actuales en el formato de carga (sirve de plantilla).</summary>
    public async Task<IActionResult> OnGetTemplateAsync()
    {
        var rows = await _context.ArticleStocks.AsNoTracking()
            .Where(s => s.Laboratory != null && s.Article != null)
            .OrderBy(s => s.Laboratory!.Code).ThenBy(s => s.Article!.Code)
            .Select(s => new { Lab = s.Laboratory!.Code, Art = s.Article!.Code, s.Article.Name, s.AvailableQuantity })
            .ToListAsync();

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("ExistenciasArticulos");
        for (var c = 0; c < Headers.Length; c++) sheet.Cells[1, c + 1].Value = Headers[c];
        sheet.Cells[1, 4].Value = "ArticuloNombre (referencia, no se importa)";
        for (var i = 0; i < rows.Count; i++)
        {
            sheet.Cells[i + 2, 1].Value = rows[i].Lab;
            sheet.Cells[i + 2, 2].Value = rows[i].Art;
            sheet.Cells[i + 2, 3].Value = rows[i].AvailableQuantity;
            sheet.Cells[i + 2, 4].Value = rows[i].Name;
        }
        sheet.Cells[1, 1, 1, 4].Style.Font.Bold = true;
        sheet.Cells.AutoFitColumns();

        return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Existencias_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        if (Upload == null || Upload.Length == 0)
        {
            TempData.Error("Selecciona un archivo Excel (.xlsx).");
            return RedirectToPage();
        }
        if (!Upload.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || Upload.Length > 10 * 1024 * 1024)
        {
            TempData.Error("El archivo debe ser .xlsx y pesar menos de 10 MB.");
            return RedirectToPage();
        }

        List<(int Row, string Lab, string Art, string Qty)> raw;
        try
        {
            raw = ReadSheet(Upload);
        }
        catch (ImportFileException ex)
        {
            TempData.Error(ex.Message);
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo leer el Excel de existencias {File}", Upload.FileName);
            TempData.Error("No se pudo leer el archivo. Verifica que sea un Excel válido.");
            return RedirectToPage();
        }

        Rows = await ValidateAsync(raw);
        FileName = Upload.FileName;
        Token = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString(SessionPrefix + Token, JsonSerializer.Serialize(Rows));
        return Page();
    }

    public async Task<IActionResult> OnPostApplyAsync()
    {
        var json = string.IsNullOrEmpty(Token) ? null : HttpContext.Session.GetString(SessionPrefix + Token);
        if (json == null)
        {
            TempData.Error("La vista previa expiró. Vuelve a subir el archivo.");
            return RedirectToPage();
        }
        HttpContext.Session.Remove(SessionPrefix + Token);

        var pending = (JsonSerializer.Deserialize<List<PreviewRow>>(json) ?? [])
            .Where(r => r.Action is RowAction.Create or RowAction.Update)
            .ToList();
        if (pending.Count == 0)
        {
            TempData.Info("No había cambios que aplicar.");
            return RedirectToPage("./Index");
        }

        int created = 0, updated = 0, skipped = 0;
        var today = DateTime.Today;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var batch in pending.Chunk(BatchSize))
            {
                var labIds = batch.Select(r => r.LaboratoryId).Distinct().ToList();
                var articleIds = batch.Select(r => r.ArticleId).Distinct().ToList();
                // Se relee el estado actual: si alguien cambió el stock tras la vista previa, se respeta el dato nuevo del Excel igual.
                var existing = await _context.ArticleStocks.AsTracking()
                    .Where(s => labIds.Contains(s.LaboratoryId) && articleIds.Contains(s.ArticleId))
                    .ToListAsync();

                foreach (var row in batch)
                {
                    var quantity = row.Quantity!.Value;
                    var stock = existing.FirstOrDefault(s => s.LaboratoryId == row.LaboratoryId && s.ArticleId == row.ArticleId);
                    if (stock == null)
                    {
                        _context.ArticleStocks.Add(new ArticleStock
                        {
                            LaboratoryId = row.LaboratoryId,
                            ArticleId = row.ArticleId,
                            AvailableQuantity = quantity,
                            UpdatedOn = today
                        });
                        created++;
                    }
                    else if (stock.AvailableQuantity != quantity)
                    {
                        stock.AvailableQuantity = quantity;
                        stock.UpdatedOn = today;
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                }

                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
            }
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Falló la carga masiva de existencias");
            TempData.Error("No se pudo aplicar la carga. No se guardó ningún cambio; intenta nuevamente.");
            return RedirectToPage();
        }

        TempData.Success($"Existencias cargadas: {created} nueva(s) y {updated} actualizada(s)."
            + (skipped > 0 ? $" {skipped} ya tenían esa cantidad." : ""));
        return RedirectToPage("./Index");
    }

    /// <summary>Lee la primera hoja (o "ExistenciasArticulos" si existe). Encabezados sin distinguir mayúsculas.</summary>
    private static List<(int Row, string Lab, string Art, string Qty)> ReadSheet(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream);
        var sheet = package.Workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("ExistenciasArticulos", StringComparison.OrdinalIgnoreCase))
            ?? package.Workbook.Worksheets.FirstOrDefault()
            ?? throw new ImportFileException("El archivo no tiene hojas.");
        if (sheet.Dimension == null) throw new ImportFileException("La hoja está vacía.");

        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var c = 1; c <= sheet.Dimension.End.Column; c++)
        {
            var header = sheet.Cells[1, c].Text?.Trim();
            if (!string.IsNullOrEmpty(header) && !columns.ContainsKey(header)) columns[header] = c;
        }
        var missing = Headers.Where(h => !columns.ContainsKey(h)).ToList();
        if (missing.Count > 0)
            throw new ImportFileException($"Faltan columnas: {string.Join(", ", missing)}. Descarga la plantilla para ver el formato.");

        var result = new List<(int, string, string, string)>();
        for (var r = 2; r <= sheet.Dimension.End.Row; r++)
        {
            var lab = sheet.Cells[r, columns["LaboratorioCodigo"]].Text?.Trim() ?? "";
            var art = sheet.Cells[r, columns["ArticuloCodigo"]].Text?.Trim() ?? "";
            var qty = sheet.Cells[r, columns["CantidadDisponible"]].Value?.ToString()?.Trim() ?? "";
            if (lab.Length == 0 && art.Length == 0 && qty.Length == 0) continue; // fila vacía
            result.Add((r, lab.ToUpperInvariant(), art.ToUpperInvariant(), qty));
            if (result.Count > MaxRows) throw new ImportFileException($"El archivo supera el máximo de {MaxRows} filas.");
        }
        if (result.Count == 0) throw new ImportFileException("El archivo no tiene filas con datos.");
        return result;
    }

    private async Task<List<PreviewRow>> ValidateAsync(List<(int Row, string Lab, string Art, string Qty)> raw)
    {
        var labCodes = raw.Select(r => r.Lab).Where(c => c.Length > 0).Distinct().ToList();
        var artCodes = raw.Select(r => r.Art).Where(c => c.Length > 0).Distinct().ToList();
        var labs = await _context.Laboratories.AsNoTracking()
            .Where(l => labCodes.Contains(l.Code) && l.Status == GeneralStatus.Activo)
            .ToDictionaryAsync(l => l.Code.ToUpper(), l => l.Id);
        var articles = new Dictionary<string, (int Id, string Name)>();
        foreach (var chunk in artCodes.Chunk(1000))
        {
            var found = await _context.Articles.AsNoTracking()
                .Where(a => chunk.Contains(a.Code) && a.Status == GeneralStatus.Activo)
                .Select(a => new { a.Id, a.Code, a.Name })
                .ToListAsync();
            foreach (var a in found) articles[a.Code.ToUpper()] = (a.Id, a.Name);
        }
        var current = new Dictionary<(int, int), decimal>();
        if (labs.Count > 0)
        {
            var labIds = labs.Values.ToList();
            foreach (var s in await _context.ArticleStocks.AsNoTracking().Where(s => labIds.Contains(s.LaboratoryId)).ToListAsync())
                current[(s.LaboratoryId, s.ArticleId)] = s.AvailableQuantity;
        }

        var seen = new HashSet<(string, string)>();
        var rows = new List<PreviewRow>();
        foreach (var (row, lab, art, qtyText) in raw)
        {
            articles.TryGetValue(art, out var article);
            string? error = null;
            decimal? quantity = null;
            if (lab.Length == 0 || art.Length == 0) error = "Falta el laboratorio o el artículo.";
            else if (!labs.ContainsKey(lab)) error = $"Laboratorio {lab} inexistente o inactivo.";
            else if (article == default) error = $"Artículo {art} inexistente o inactivo.";
            else if (!TryParseQuantity(qtyText, out var q)) error = "Cantidad vacía o no numérica.";
            else if (q < 0 || q > 999999) error = "La cantidad debe estar entre 0 y 999.999.";
            else if (!seen.Add((lab, art))) error = "Fila repetida (mismo laboratorio y artículo).";
            else quantity = q;

            if (error != null)
            {
                rows.Add(new PreviewRow(row, lab, art, article.Name, null, null, RowAction.Error, error));
                continue;
            }

            var labId = labs[lab];
            decimal? existing = current.TryGetValue((labId, article.Id), out var e) ? e : null;
            var action = existing == null ? RowAction.Create : existing == quantity ? RowAction.Unchanged : RowAction.Update;
            rows.Add(new PreviewRow(row, lab, art, article.Name, quantity, existing, action, null, labId, article.Id));
        }
        return rows;
    }

    /// <summary>Acepta punto o coma decimal.</summary>
    private static bool TryParseQuantity(string text, out decimal value) =>
        decimal.TryParse(text.Replace(',', '.'), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out value);

    /// <summary>Error de formato con mensaje en español pensado para el usuario (los de EPPlus vienen en inglés y no se muestran).</summary>
    private sealed class ImportFileException : Exception
    {
        public ImportFileException(string message) : base(message) { }
    }
}
