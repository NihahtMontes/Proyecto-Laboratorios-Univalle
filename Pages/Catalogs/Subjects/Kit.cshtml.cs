using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Subjects
{
    /// <summary>
    /// Kit de la materia: artículos y equipos (como cantidad) que la materia usa habitualmente.
    /// En Requerimientos (paso 2) se cargan con "Cargar kit de la materia".
    /// </summary>
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class KitModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly RequirementPickerService _picker;

        public KitModel(ApplicationDbContext context, RequirementPickerService picker)
        {
            _context = context;
            _picker = picker;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        /// <summary>Selección del buscador: artículos y equipos del catálogo, ambos con cantidad.</summary>
        [BindProperty]
        public KitBatchInput Batch { get; set; } = new();

        public Subject? Subject { get; private set; }
        public List<KitItemRow> Items { get; private set; } = [];
        public List<RequirementPickerService.CategoryOption> Categories { get; private set; } = [];

        public sealed record KitItemRow(int Id, string ArticleCode, string ArticleName, string? Category, string? Unit,
            bool IsEquipment, bool Pending, decimal Quantity, bool IsScalable, string? Observations);

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await LoadAsync()) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAddBatchAsync()
        {
            if (!await SubjectExistsAsync()) return NotFound();

            var articleLines = Batch.Articles.Where(l => l.Id > 0).ToList();
            var equipmentLines = Batch.Equipments.Where(l => l.Id > 0).ToList();
            if (articleLines.Count == 0 && equipmentLines.Count == 0)
            {
                TempData.Warning("Selecciona al menos un artículo o equipo.");
                return RedirectToPage(new { id = Id });
            }
            if (articleLines.Concat(equipmentLines).Any(l => l.Quantity is not decimal q || q <= 0 || q > 999999 || decimal.Round(q, 2) != q))
            {
                TempData.Error("Las cantidades deben ser mayores a 0 y admiten hasta 2 decimales.");
                return RedirectToPage(new { id = Id });
            }

            var articleIds = articleLines.Select(l => l.Id).Distinct().ToList();
            if (await _context.Articles.AsNoTracking().CountAsync(a => articleIds.Contains(a.Id) && a.Status == GeneralStatus.Activo) != articleIds.Count)
            {
                TempData.Error("Algún artículo seleccionado ya no está activo. Vuelve a buscarlo.");
                return RedirectToPage(new { id = Id });
            }
            var equipmentIds = equipmentLines.Select(l => l.Id).Distinct().ToList();
            if (await _context.Equipments.AsNoTracking().CountAsync(e => equipmentIds.Contains(e.Id) && e.Status == GeneralStatus.Activo) != equipmentIds.Count)
            {
                TempData.Error("Algún equipo seleccionado ya no está activo. Vuelve a buscarlo.");
                return RedirectToPage(new { id = Id });
            }

            int added = 0, merged = 0;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Los equipos entran al kit como su artículo vinculado (se resuelve o crea; guarda).
                var lines = articleLines.Select(l => (ArticleId: l.Id, Quantity: l.Quantity!.Value)).ToList();
                foreach (var line in equipmentLines)
                    lines.Add((await _picker.ResolveEquipmentArticleAsync(line.Id), line.Quantity!.Value));

                var kit = await _context.SubjectKitItems.AsTracking().Where(k => k.SubjectId == Id).ToListAsync();

                // Tope de 999999 también al acumular (antes un artículo repetido podía sumar 1.000.005 en el kit).
                foreach (var group in lines.GroupBy(l => l.ArticleId))
                {
                    var current = kit.FirstOrDefault(k => k.ArticleId == group.Key)?.Quantity ?? 0m;
                    if (current + group.Sum(l => l.Quantity) > 999999m)
                    {
                        await transaction.RollbackAsync();
                        TempData.Error("La cantidad total de un artículo en el kit no puede superar 999999.");
                        return RedirectToPage(new { id = Id });
                    }
                }

                foreach (var group in lines.GroupBy(l => l.ArticleId))
                {
                    var quantity = group.Sum(l => l.Quantity);
                    var item = kit.FirstOrDefault(k => k.ArticleId == group.Key);
                    if (item != null)
                    {
                        item.Quantity += quantity;
                        merged++;
                    }
                    else
                    {
                        item = new SubjectKitItem { SubjectId = Id, ArticleId = group.Key, Quantity = quantity };
                        _context.SubjectKitItems.Add(item);
                        kit.Add(item);
                        added++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                TempData.Error("No se pudo actualizar el kit. Intente nuevamente.");
                return RedirectToPage(new { id = Id });
            }

            var parts = new List<string>();
            if (added > 0) parts.Add($"{added} artículo(s) agregado(s)");
            if (merged > 0) parts.Add($"{merged} cantidad(es) sumada(s)");
            TempData.Success($"Kit actualizado: {string.Join(" y ", parts)}.");
            return RedirectToPage(new { id = Id });
        }

        public async Task<IActionResult> OnPostUpdateAsync(int itemId, decimal quantity, bool isScalable, string? observations)
        {
            if (quantity <= 0 || quantity > 999999 || decimal.Round(quantity, 2) != quantity)
            {
                TempData.Error("La cantidad debe ser mayor a 0 y admite hasta 2 decimales.");
                return RedirectToPage(new { id = Id });
            }
            var item = await _context.SubjectKitItems.AsTracking().FirstOrDefaultAsync(k => k.Id == itemId && k.SubjectId == Id);
            if (item == null)
            {
                TempData.Error("La línea no pertenece al kit de esta materia.");
                return RedirectToPage(new { id = Id });
            }

            var note = observations.Clean();
            item.Quantity = quantity;
            item.IsScalable = isScalable;
            item.Observations = string.IsNullOrWhiteSpace(note) ? null : note[..Math.Min(note.Length, 500)];
            await _context.SaveChangesAsync();
            TempData.Success("Línea del kit actualizada.");
            return RedirectToPage(new { id = Id });
        }

        public async Task<IActionResult> OnPostRemoveAsync(int itemId)
        {
            var item = await _context.SubjectKitItems.AsTracking().FirstOrDefaultAsync(k => k.Id == itemId && k.SubjectId == Id);
            if (item != null)
            {
                item.IsRemoved = true;
                await _context.SaveChangesAsync();
                TempData.Success("Artículo retirado del kit.");
            }
            return RedirectToPage(new { id = Id });
        }

        private Task<bool> SubjectExistsAsync() =>
            _context.Subjects.AsNoTracking().AnyAsync(s => s.Id == Id && s.Status != GeneralStatus.Eliminado);

        private async Task<bool> LoadAsync()
        {
            Subject = await _context.Subjects.AsNoTracking()
                .Include(s => s.Career)
                .FirstOrDefaultAsync(s => s.Id == Id && s.Status != GeneralStatus.Eliminado);
            if (Subject == null) return false;

            Items = await _context.SubjectKitItems.AsNoTracking()
                .Where(k => k.SubjectId == Id)
                .OrderBy(k => k.Article!.Name)
                .Select(k => new KitItemRow(k.Id, k.Article!.Code, k.Article.Name,
                    k.Article.ArticleCategory != null ? k.Article.ArticleCategory.Name : k.Article.Category,
                    k.Article.MeasurementUnit != null ? k.Article.MeasurementUnit.Name : k.Article.UnitOfMeasure,
                    k.Article.EquipmentId != null,
                    k.Article.ReviewStatus == DataReviewStatus.PorConfirmar,
                    k.Quantity, k.IsScalable, k.Observations))
                .ToListAsync();
            Categories = await _picker.CategoriesAsync();
            return true;
        }

        public sealed class KitBatchInput
        {
            public List<KitBatchLine> Articles { get; set; } = [];
            public List<KitBatchLine> Equipments { get; set; } = [];
        }

        public sealed class KitBatchLine
        {
            public int Id { get; set; }
            public decimal? Quantity { get; set; }
        }
    }
}
