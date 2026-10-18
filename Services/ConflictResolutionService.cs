using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>Resultado de reducir la cantidad pedida de un artículo en una planificación.</summary>
public sealed record ConflictResolutionResult(bool Ok, string Message);

public interface IConflictResolutionService
{
    /// <summary>
    /// Reduce la cantidad pedida del artículo en la planificación (mismas reglas que Requerimientos:
    /// gestión escribible, planificación Planificada y una sola línea del artículo sin unidad física).
    /// </summary>
    Task<ConflictResolutionResult> ReduceQuantityAsync(int planningId, int articleId, decimal newQuantity, CancellationToken ct = default);
}

public sealed class ConflictResolutionService : IConflictResolutionService
{
    private static readonly CultureInfo Cultura = new("es-ES");

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;

    public ConflictResolutionService(ApplicationDbContext context, IAcademicManagementService managements)
    {
        _context = context;
        _managements = managements;
    }

    public async Task<ConflictResolutionResult> ReduceQuantityAsync(int planningId, int articleId, decimal newQuantity, CancellationToken ct = default)
    {
        // Solo gestiones activas o inactivas admiten cambios.
        if (!await PlanningContextLoader.IsWritableAsync(_managements, planningId))
            return new(false, PlanningContextLoader.ReadOnlyMessage);

        var plan = await _context.Plannings.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == planningId && !r.IsDeleted, ct);
        if (plan == null)
            return new(false, "La planificación no existe.");
        if (plan.Status != PlanningStatus.Planned)
            return new(false, "La planificación ya fue aprobada; sus requerimientos no se pueden modificar.");

        // La cantidad vive en la única línea del artículo sin unidad física; con más líneas o unidades se edita en Materiales.
        var lines = await _context.PlanningRequirements.AsNoTracking()
            .Where(d => d.PlanningId == planningId && d.ArticleId == articleId && d.EquipmentUnitId == null)
            .Select(d => new { d.Id, d.RequestedQuantity, ArticleName = d.Article != null ? d.Article.Name : null })
            .ToListAsync(ct);
        if (lines.Count != 1 || lines[0].RequestedQuantity is not decimal current)
            return new(false, "Tiene varias líneas o unidades físicas: edítalo en Materiales.");

        if (newQuantity <= 0 || newQuantity > 999999 || decimal.Round(newQuantity, 2) != newQuantity)
            return new(false, "La cantidad debe ser mayor a 0 y admite hasta 2 decimales.");
        if (newQuantity >= current)
            return new(false, $"La cantidad debe ser menor a la actual ({current.ToString("0.##", Cultura)}).");

        var detail = await _context.PlanningRequirements.AsTracking()
            .FirstOrDefaultAsync(d => d.Id == lines[0].Id, ct);
        if (detail == null)
            return new(false, "El requerimiento no pertenece a esta planificación.");

        detail.RequestedQuantity = newQuantity;
        detail.ExpectedQuantity = (int)Math.Ceiling(newQuantity);
        await _context.SaveChangesAsync(ct);

        return new(true,
            $"Cantidad de {lines[0].ArticleName ?? articleId.ToString()} en {L3Codes.Format(planningId)} " +
            $"reducida de {current.ToString("0.##", Cultura)} a {newQuantity.ToString("0.##", Cultura)}.");
    }
}
