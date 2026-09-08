using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public sealed record EquipmentClassificationDecisionInput(
        string DecisionKey,
        string CatalogCode,
        EquipmentClassificationReviewStatus ReviewStatus,
        EquipmentCategory? Category,
        EquipmentTypeClassification? TypeClassification,
        UtensilType? UtensilType,
        GeneralStatus? GeneralStatus,
        string? OtherDetail,
        string? EvidenceReference,
        int? ResponsiblePersonId,
        string? ResponsibleSnapshot,
        DateTime? DecisionDate,
        DateTime? EffectiveFrom,
        int? ImportBatchId,
        long? ImportSourceRowId,
        int? RecordedByUserId);

    public sealed record EquipmentClassificationDecisionResult(
        long DecisionId,
        bool Applied,
        EquipmentClassificationReviewStatus ReviewStatus);

    public interface IEquipmentClassificationDecisionService
    {
        Task<EquipmentClassificationDecisionResult> ApplyAsync(
            EquipmentClassificationDecisionInput input,
            CancellationToken cancellationToken = default);

        Task<EquipmentClassificationDecision?> GetEffectiveDecisionAsync(
            int equipmentId,
            DateTime eventDate,
            CancellationToken cancellationToken = default);
    }

    public sealed class EquipmentClassificationDecisionService : IEquipmentClassificationDecisionService
    {
        private readonly ApplicationDbContext _context;

        public EquipmentClassificationDecisionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EquipmentClassificationDecisionResult> ApplyAsync(
            EquipmentClassificationDecisionInput input,
            CancellationToken cancellationToken = default)
        {
            var decisionKey = Required(input.DecisionKey, nameof(input.DecisionKey), 200);
            var catalogCode = Required(input.CatalogCode, nameof(input.CatalogCode), 30);

            var existingDecision = await _context.EquipmentClassificationDecisions
                .AsNoTracking()
                .SingleOrDefaultAsync(decision => decision.DecisionKey == decisionKey, cancellationToken);

            if (existingDecision != null)
            {
                return new EquipmentClassificationDecisionResult(
                    existingDecision.Id,
                    false,
                    existingDecision.ReviewStatus);
            }

            var equipment = await _context.Equipments
                .IgnoreQueryFilters()
                .AsTracking()
                .SingleOrDefaultAsync(item => item.CatalogCode == catalogCode, cancellationToken)
                ?? throw new InvalidOperationException($"No existe el catálogo {catalogCode}.");

            Validate(input);

            await using var transaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            var activeDecisions = await _context.EquipmentClassificationDecisions
                .AsTracking()
                .Where(decision => decision.EquipmentId == equipment.Id && decision.EffectiveTo == null)
                .ToListAsync(cancellationToken);

            var closingDate = input.EffectiveFrom ?? input.DecisionDate ?? DateTime.UtcNow;
            foreach (var activeDecision in activeDecisions)
            {
                if (activeDecision.EffectiveFrom.HasValue && closingDate <= activeDecision.EffectiveFrom.Value)
                {
                    throw new InvalidOperationException(
                        "La nueva vigencia debe ser posterior al inicio de la decisión actual.");
                }

                activeDecision.EffectiveTo = closingDate;
            }

            if (activeDecisions.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            var decision = new EquipmentClassificationDecision
            {
                DecisionKey = decisionKey,
                EquipmentId = equipment.Id,
                ReviewStatus = input.ReviewStatus,
                Category = input.Category,
                TypeClassification = input.TypeClassification,
                UtensilType = input.UtensilType,
                GeneralStatus = input.GeneralStatus,
                OtherDetail = Clean(input.OtherDetail, 1000),
                EvidenceReference = Clean(input.EvidenceReference, 1000),
                ResponsiblePersonId = input.ResponsiblePersonId,
                ResponsibleSnapshot = Clean(input.ResponsibleSnapshot, 200),
                DecisionDate = input.DecisionDate,
                EffectiveFrom = input.EffectiveFrom,
                ImportBatchId = input.ImportBatchId,
                ImportSourceRowId = input.ImportSourceRowId,
                RecordedByUserId = input.RecordedByUserId
            };

            _context.EquipmentClassificationDecisions.Add(decision);

            equipment.ClassificationReviewStatus = input.ReviewStatus;
            if (input.ReviewStatus == EquipmentClassificationReviewStatus.Confirmed)
            {
                equipment.Category = input.Category!.Value;
                equipment.TypeClassification = input.TypeClassification;
                equipment.UtensilType = input.UtensilType;
                equipment.Status = input.GeneralStatus!.Value;
                equipment.OtherClassificationDetail = Clean(input.OtherDetail, 1000);
            }

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new EquipmentClassificationDecisionResult(
                decision.Id,
                true,
                decision.ReviewStatus);
        }

        public Task<EquipmentClassificationDecision?> GetEffectiveDecisionAsync(
            int equipmentId,
            DateTime eventDate,
            CancellationToken cancellationToken = default)
        {
            return _context.EquipmentClassificationDecisions
                .AsNoTracking()
                .Where(decision => decision.EquipmentId == equipmentId
                    && (!decision.EffectiveFrom.HasValue || decision.EffectiveFrom.Value <= eventDate)
                    && (!decision.EffectiveTo.HasValue || decision.EffectiveTo.Value > eventDate))
                .OrderByDescending(decision => decision.ReviewStatus == EquipmentClassificationReviewStatus.Confirmed)
                .ThenByDescending(decision => decision.EffectiveFrom)
                .ThenByDescending(decision => decision.DecisionDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static void Validate(EquipmentClassificationDecisionInput input)
        {
            if (!Enum.IsDefined(input.ReviewStatus))
            {
                throw new ArgumentOutOfRangeException(nameof(input.ReviewStatus));
            }

            if (input.ReviewStatus != EquipmentClassificationReviewStatus.Confirmed)
            {
                return;
            }

            if (!input.Category.HasValue
                || !input.GeneralStatus.HasValue
                || !EquipmentClassificationRules.IsConfirmedCombinationValid(
                    input.Category.Value,
                    input.TypeClassification,
                    input.UtensilType,
                    input.OtherDetail))
            {
                throw new InvalidOperationException("La jerarquía de clasificación confirmada no es válida.");
            }

            if (string.IsNullOrWhiteSpace(input.EvidenceReference)
                || string.IsNullOrWhiteSpace(input.ResponsibleSnapshot)
                || !input.DecisionDate.HasValue)
            {
                throw new InvalidOperationException(
                    "Una clasificación confirmada exige evidencia, responsable y fecha de decisión.");
            }
        }

        private static string Required(string? value, string fieldName, int maxLength)
        {
            var clean = Clean(value, maxLength);
            return string.IsNullOrWhiteSpace(clean)
                ? throw new ArgumentException("El valor es obligatorio.", fieldName)
                : clean;
        }

        private static string? Clean(string? value, int maxLength)
        {
            var clean = value?.Trim();
            if (string.IsNullOrWhiteSpace(clean))
            {
                return null;
            }

            return clean.Length <= maxLength ? clean : clean[..maxLength];
        }
    }
}
