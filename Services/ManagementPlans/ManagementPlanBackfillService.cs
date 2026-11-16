using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Services.ManagementPlans
{
    /// <summary>
    /// Reconstrucción de planes L-48 desde registros existentes.
    /// <para>
    /// Candidatos: un plan por cada par (ManagementId, EquipmentUnitId) que aparezca en verificaciones L-6
    /// (no anuladas ni en borrador), solicitudes técnicas/calibración L-7 (por EquipmentUnitId o por
    /// RequestEquipmentUnits con DeactivatedDate null), mantenimientos L-8, salidas L-3 con equipo y sin
    /// PlanningId (las de PlanningId son material de prácticas y se ignoran) y solicitudes de compra L-12
    /// (solo para AcquisitionRequestId). Se excluyen los registros con estado Cancelado/Anulada y los
    /// equipos inexistentes o dados de baja (Decommissioned/Dismantled/Deleted). Si el par ya tiene un
    /// ManagementPlan se omite (idempotente, respeta el índice único ManagementPlans.ManagementId+EquipmentUnitId).
    /// </para>
    /// <para>
    /// Enlaces: el registro más reciente de cada tipo para el par (por fecha y luego Id). Si el
    /// mantenimiento tiene RequestId de una solicitud técnica, esa solicitud es la preferida como RequestId.
    /// Solo las salidas por mantenimiento (tipo 3/5) se enlazan como DepartureId, como en el wizard y en la
    /// importación histórica; una salida que solo es préstamo no enlaza.
    /// </para>
    /// <para>
    /// Regla de fase/estado/PlanStatus (copia de Tools/HistoricalDataImport.ResolvePlanState, contrastada
    /// con el alta real de la app), evaluada en orden: L-12 desembolso → (Disbursement, Completed, Completed);
    /// L-3 de mantenimiento → (Kardex, AwaitingKardex, InProgress); L-8 completado → (Exit, AwaitingDeparture,
    /// Completed); L-8 sin completar → (Maintenance, InMaintenance si está en curso o con fecha de inicio,
    /// si no AwaitingMaintenance, InProgress); solo L-7 técnica → (Maintenance, AwaitingMaintenance, InProgress);
    /// L-6 con fallas → (TechnicalRequest, AwaitingRequest, InProgress); L-6 sin fallas → (Verification,
    /// VerifiedGood, InProgress), igual que Verifications/Create (la importación histórica asumía falla
    /// inferida; aquí se mira si la verificación realmente tiene fallas u observaciones).
    /// PlannedDate = fecha del primer registro de la cadena; ResponsiblePersonId = el de la verificación.
    /// </para>
    /// </summary>
    public sealed class ManagementPlanBackfillService : IManagementPlanBackfillService
    {
        public const string SourceType = "ReconstruccionPlanes";
        private const int BatchSize = 100;

        private readonly ApplicationDbContext _context;
        private readonly IManagementContextService _managementContext;
        private readonly ILogger<ManagementPlanBackfillService> _logger;

        public ManagementPlanBackfillService(ApplicationDbContext context,
            IManagementContextService managementContext,
            ILogger<ManagementPlanBackfillService> logger)
        {
            _context = context;
            _managementContext = managementContext;
            _logger = logger;
        }

        // ───────────────────────────────────────────────────────── vista previa

        public async Task<PlanBackfillPreview> PreviewAsync(int? managementId, CancellationToken ct = default)
        {
            var build = await BuildAsync(managementId, ct);
            var preview = new PlanBackfillPreview();

            foreach (var management in build.Managements)
            {
                var specs = build.NewPlans.Where(p => p.ManagementId == management.Id).ToList();
                preview.Managements.Add(new PlanBackfillManagementSummary
                {
                    ManagementId = management.Id,
                    Code = management.Code,
                    Type = management.Type,
                    ExistingPlans = build.ExistingPlanCounts.GetValueOrDefault(management.Id),
                    CandidateUnits = build.CandidatePairs.GetValueOrDefault(management.Id),
                    NewPlans = specs.Count,
                    NewPlansByState = specs.GroupBy(p => p.State).ToDictionary(g => g.Key, g => g.Count())
                });
            }

            return preview;
        }

        // ───────────────────────────────────────────────────────── carga

        public async Task<PlanBackfillResult> ApplyAsync(int? managementId, int? userId, CancellationToken ct = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                // Se revisa dentro de la transacción: entre la vista previa y la confirmación pudo cambiar algo.
                var build = await BuildAsync(managementId, ct);
                if (build.NewPlans.Count == 0)
                {
                    return new PlanBackfillResult
                    {
                        Ok = true,
                        Message = "No hay planes por crear: cada equipo con registros ya tiene su plan en la gestión seleccionada."
                    };
                }

                var code = $"PLANES-{DateTime.UtcNow:yyyyMMddHHmmss}";
                var affectedManagements = build.NewPlans.Select(p => p.ManagementId).Distinct().Count();
                var batch = new ImportBatch
                {
                    Code = code,
                    SourceType = SourceType,
                    ContractVersion = "planes-v1",
                    SourceName = "Reconstrucción de planes desde registros",
                    SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))),
                    ImportedAt = DateTime.UtcNow,
                    Status = ImportBatchStatus.Official,
                    Notes = $"Reconstrucción de planes L-48: {build.NewPlans.Count} planes nuevos en " +
                            $"{affectedManagements} gestión(es). No se modificó ningún registro existente." +
                            (userId.HasValue ? $" Ejecutado por el usuario #{userId}." : string.Empty)
                };
                _context.ImportBatches.Add(batch);
                await _context.SaveChangesAsync(ct);

                // Lotes de 100 (regla del proyecto), todos dentro de la misma transacción.
                foreach (var chunk in build.NewPlans.Chunk(BatchSize))
                {
                    foreach (var spec in chunk)
                    {
                        _context.ManagementPlans.Add(new ManagementPlan
                        {
                            ManagementId = spec.ManagementId,
                            EquipmentUnitId = spec.EquipmentUnitId,
                            VerificationId = spec.VerificationId,
                            RequestId = spec.RequestId,
                            MaintenanceId = spec.MaintenanceId,
                            DepartureId = spec.DepartureId,
                            AcquisitionRequestId = spec.AcquisitionRequestId,
                            CurrentPhase = spec.Phase,
                            CurrentState = spec.State,
                            PlanStatus = spec.PlanStatus,
                            PlannedDate = spec.PlannedDate,
                            ResponsiblePersonId = spec.ResponsiblePersonId,
                            IsDraft = false,
                            // CreatedById/CreatedDate los estampa ApplicationDbContext al guardar (auditoría centralizada).
                            ImportBatchId = batch.Id
                        });
                    }
                    await _context.SaveChangesAsync(ct);
                    _context.ChangeTracker.Clear();
                }

                await transaction.CommitAsync(ct);
                _managementContext.InvalidateCache();
                _logger.LogInformation(
                    "Planes reconstruidos: {Created} planes en {Managements} gestión(es), lote {Batch}, usuario {UserId}",
                    build.NewPlans.Count, affectedManagements, batch.Code, userId);

                return new PlanBackfillResult
                {
                    Ok = true,
                    CreatedPlans = build.NewPlans.Count,
                    AffectedManagements = affectedManagements,
                    ImportBatchId = batch.Id,
                    ImportBatchCode = batch.Code,
                    Message = $"Se reconstruyeron {build.NewPlans.Count} planes en {affectedManagements} gestión(es)."
                };
            }
            catch (Exception ex) when (ex is DbUpdateException or ArgumentException or InvalidOperationException)
            {
                try { await transaction.RollbackAsync(ct); } catch (InvalidOperationException) { }
                _context.ChangeTracker.Clear();
                _logger.LogError(ex, "No se pudieron reconstruir los planes (gestión {ManagementId})", managementId);
                return new PlanBackfillResult
                {
                    Ok = false,
                    Message = "No se reconstruyó nada: ocurrió un error al guardar los planes. Intente nuevamente."
                };
            }
        }

        // ───────────────────────────────────────────────────────── construcción de cadenas

        private sealed record ManagementRef(int Id, string Code, ManagementType Type);

        private sealed record ChainSpec(
            int ManagementId,
            int EquipmentUnitId,
            int? VerificationId,
            int? RequestId,
            int? MaintenanceId,
            int? DepartureId,
            int? AcquisitionRequestId,
            WizardPhase Phase,
            WizardEquipmentState State,
            ManagementPlanStatus PlanStatus,
            DateTime? PlannedDate,
            int? ResponsiblePersonId);

        private sealed record VerRow(int Id, int ManagementId, int UnitId, DateTime Date, bool HasFailures, int? ResponsiblePersonId);
        private sealed record ReqRow(int Id, int ManagementId, int? UnitId, RequestType Type, DateTime Date);
        private sealed record MntRow(int Id, int ManagementId, int UnitId, DateTime Date, int? RequestId, MaintenanceStatus Status, DateTime? StartDate);
        private sealed record DepRow(int Id, int ManagementId, int UnitId, DateTime Date, DepartureType Type);

        /// <summary>Acumulador del par (gestión, equipo): el registro más reciente de cada tipo.</summary>
        private sealed class Chain
        {
            public VerRow? Verification;
            public ReqRow? TechnicalRequest;
            public ReqRow? AcquisitionRequest;
            public MntRow? Maintenance;
            public DepRow? Departure;
            public DateTime? FirstDate;
        }

        private sealed class BuildResult
        {
            public List<ManagementRef> Managements { get; } = [];
            public Dictionary<int, int> ExistingPlanCounts { get; } = [];
            public Dictionary<int, int> CandidatePairs { get; } = [];
            public List<ChainSpec> NewPlans { get; } = [];
        }

        private async Task<BuildResult> BuildAsync(int? managementId, CancellationToken ct)
        {
            var result = new BuildResult();

            var managementQuery = _context.Managements.AsNoTracking();
            if (managementId.HasValue)
                managementQuery = managementQuery.Where(m => m.Id == managementId.Value);
            var managements = await managementQuery
                .OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester).ThenBy(m => m.Code)
                .Select(m => new ManagementRef(m.Id, m.Code, m.Type))
                .ToListAsync(ct);
            if (managements.Count == 0)
                return result;
            var managementIds = managements.Select(m => m.Id).ToHashSet();
            foreach (var management in managements)
            {
                result.Managements.Add(management);
                result.ExistingPlanCounts[management.Id] = 0;
            }

            // ── Registros fuente (proyecciones planas; volumen histórico acotado)
            var verifications = await _context.Verifications.AsNoTracking()
                .Where(v => managementIds.Contains(v.ManagementId)
                    && v.Status != VerificationStatus.Annulled
                    && v.Status != VerificationStatus.Draft)
                .Select(v => new VerRow(v.Id, v.ManagementId, v.EquipmentUnitId, v.Date,
                    (v.Faults != null && v.Faults.Any(f => !f.IsDeleted))
                        || (v.Observations != null && v.Observations != ""),
                    v.ResponsiblePersonId))
                .ToListAsync(ct);

            var maintenances = await _context.Maintenances.AsNoTracking()
                .Where(m => managementIds.Contains(m.ManagementId) && m.Status != MaintenanceStatus.Cancelled)
                .Select(m => new MntRow(m.Id, m.ManagementId, m.EquipmentUnitId,
                    m.EndDate ?? m.StartDate ?? m.ScheduledDate ?? m.CreatedDate,
                    m.RequestId, m.Status, m.StartDate))
                .ToListAsync(ct);

            // Las solicitudes también se cargan por las referenciadas por un mantenimiento (RequestId preferido).
            var maintenanceRequestIds = maintenances.Where(m => m.RequestId.HasValue).Select(m => m.RequestId!.Value).ToList();
            var requests = await _context.Requests.AsNoTracking()
                .Where(r => (managementIds.Contains(r.ManagementId) || maintenanceRequestIds.Contains(r.Id))
                    && r.Status != RequestStatus.Cancelled)
                .Select(r => new ReqRow(r.Id, r.ManagementId, r.EquipmentUnitId, r.Type, r.RequestDate ?? r.CreatedDate))
                .ToListAsync(ct);

            var departures = await _context.Departures.AsNoTracking()
                .Where(d => managementIds.Contains(d.ManagementId) && d.Status != LoanStatus.Cancelled
                    && d.EquipmentUnitId != null && d.PlanningId == null)
                .Select(d => new DepRow(d.Id, d.ManagementId, d.EquipmentUnitId!.Value, d.DepartureDate, d.Type))
                .ToListAsync(ct);

            // Unidades por solicitud: EquipmentUnitId directo y, si no, los vínculos activos.
            var requestIds = requests.Select(r => r.Id).ToList();
            var requestLinks = await _context.RequestEquipmentUnits.AsNoTracking()
                .Where(l => requestIds.Contains(l.RequestId) && l.DeactivatedDate == null)
                .Select(l => new { l.RequestId, l.EquipmentUnitId })
                .ToListAsync(ct);
            var linksByRequest = requestLinks.GroupBy(l => l.RequestId)
                .ToDictionary(g => g.Key, g => g.Select(l => l.EquipmentUnitId).ToHashSet());
            var requestsById = requests.ToDictionary(r => r.Id);

            // Equipos válidos: existen y no están dados de baja.
            var unitIds = new HashSet<int>();
            unitIds.UnionWith(verifications.Select(v => v.UnitId));
            unitIds.UnionWith(maintenances.Select(m => m.UnitId));
            unitIds.UnionWith(departures.Select(d => d.UnitId));
            foreach (var request in requests.Where(r => managementIds.Contains(r.ManagementId)))
            {
                if (request.UnitId.HasValue) unitIds.Add(request.UnitId.Value);
                else if (linksByRequest.TryGetValue(request.Id, out var linked)) unitIds.UnionWith(linked);
            }
            var validUnits = (await _context.EquipmentUnits.AsNoTracking()
                .Where(u => unitIds.Contains(u.Id)
                    && u.CurrentStatus != EquipmentStatus.Decommissioned
                    && u.CurrentStatus != EquipmentStatus.Dismantled
                    && u.CurrentStatus != EquipmentStatus.Deleted)
                .Select(u => u.Id)
                .ToListAsync(ct)).ToHashSet();

            // Pares con plan existente: se omiten (idempotencia e índice único de ManagementPlans).
            var existingPairs = (await _context.ManagementPlans.AsNoTracking()
                .Where(p => managementIds.Contains(p.ManagementId) && p.EquipmentUnitId != null)
                .Select(p => new { p.ManagementId, p.EquipmentUnitId })
                .ToListAsync(ct))
                .Select(p => (p.ManagementId, p.EquipmentUnitId!.Value))
                .ToHashSet();
            foreach (var group in existingPairs.GroupBy(p => p.ManagementId))
                result.ExistingPlanCounts[group.Key] = group.Count();

            var chains = new Dictionary<(int ManagementId, int UnitId), Chain>();

            Chain ChainOf(int managementKeyId, int unitId)
            {
                var key = (managementKeyId, unitId);
                if (!chains.TryGetValue(key, out var chain))
                {
                    chain = new Chain();
                    chains[key] = chain;
                }
                return chain;
            }

            void Touch(int managementKeyId, int unitId, DateTime date)
            {
                var chain = ChainOf(managementKeyId, unitId);
                if (chain.FirstDate == null || date < chain.FirstDate)
                    chain.FirstDate = date;
            }

            static bool IsNewer(DateTime date, int id, DateTime currentDate, int currentId) =>
                date > currentDate || (date == currentDate && id > currentId);

            foreach (var v in verifications)
            {
                if (!validUnits.Contains(v.UnitId)) continue;
                var chain = ChainOf(v.ManagementId, v.UnitId);
                if (chain.Verification == null || IsNewer(v.Date, v.Id, chain.Verification.Date, chain.Verification.Id))
                    chain.Verification = v;
                Touch(v.ManagementId, v.UnitId, v.Date);
            }

            foreach (var request in requests.Where(r => managementIds.Contains(r.ManagementId)))
            {
                // Unidad directa o, si es null, los vínculos activos de la solicitud.
                var units = request.UnitId.HasValue
                    ? [request.UnitId.Value]
                    : linksByRequest.GetValueOrDefault(request.Id) ?? [];
                foreach (var unitId in units)
                {
                    if (!validUnits.Contains(unitId)) continue;
                    var chain = ChainOf(request.ManagementId, unitId);
                    if (request.Type == RequestType.Purchasing)
                    {
                        if (chain.AcquisitionRequest == null
                            || IsNewer(request.Date, request.Id, chain.AcquisitionRequest.Date, chain.AcquisitionRequest.Id))
                            chain.AcquisitionRequest = request;
                    }
                    else if (chain.TechnicalRequest == null
                        || IsNewer(request.Date, request.Id, chain.TechnicalRequest.Date, chain.TechnicalRequest.Id))
                    {
                        chain.TechnicalRequest = request;
                    }
                    Touch(request.ManagementId, unitId, request.Date);
                }
            }

            foreach (var m in maintenances)
            {
                if (!validUnits.Contains(m.UnitId)) continue;
                var chain = ChainOf(m.ManagementId, m.UnitId);
                if (chain.Maintenance == null || IsNewer(m.Date, m.Id, chain.Maintenance.Date, chain.Maintenance.Id))
                    chain.Maintenance = m;
                Touch(m.ManagementId, m.UnitId, m.Date);
            }

            foreach (var d in departures)
            {
                if (!validUnits.Contains(d.UnitId)) continue;
                var chain = ChainOf(d.ManagementId, d.UnitId);
                // Solo las salidas por mantenimiento enlazan como L-3 del plan (como en el wizard).
                if (d.Type is DepartureType.ExternalMaintenance or DepartureType.InternalMaintenance)
                {
                    if (chain.Departure == null || IsNewer(d.Date, d.Id, chain.Departure.Date, chain.Departure.Id))
                        chain.Departure = d;
                }
                Touch(d.ManagementId, d.UnitId, d.Date);
            }

            foreach (var ((keyManagementId, keyUnitId), chain) in chains)
            {
                result.CandidatePairs[keyManagementId] = result.CandidatePairs.GetValueOrDefault(keyManagementId) + 1;
                if (existingPairs.Contains((keyManagementId, keyUnitId)))
                    continue;

                // RequestId preferido: la solicitud técnica asociada al mantenimiento.
                int? requestId = null;
                if (chain.Maintenance?.RequestId is int maintenanceRequestId
                    && requestsById.TryGetValue(maintenanceRequestId, out var preferred)
                    && preferred.Type != RequestType.Purchasing)
                {
                    requestId = preferred.Id;
                }
                requestId ??= chain.TechnicalRequest?.Id;

                var (phase, state, planStatus) = ResolveState(chain);
                result.NewPlans.Add(new ChainSpec(
                    keyManagementId,
                    keyUnitId,
                    chain.Verification?.Id,
                    requestId,
                    chain.Maintenance?.Id,
                    chain.Departure?.Id,
                    chain.AcquisitionRequest?.Id,
                    phase,
                    state,
                    planStatus,
                    chain.FirstDate,
                    chain.Verification?.ResponsiblePersonId));
            }

            result.NewPlans.Sort((a, b) => a.ManagementId != b.ManagementId
                ? a.ManagementId.CompareTo(b.ManagementId)
                : a.EquipmentUnitId.CompareTo(b.EquipmentUnitId));
            return result;
        }

        /// <summary>Regla de fase/estado documentada en el XML de la clase.</summary>
        private static (WizardPhase Phase, WizardEquipmentState State, ManagementPlanStatus PlanStatus) ResolveState(Chain chain)
        {
            if (chain.AcquisitionRequest != null)
                return (WizardPhase.Disbursement, WizardEquipmentState.Completed, ManagementPlanStatus.Completed);
            if (chain.Departure != null)
                return (WizardPhase.Kardex, WizardEquipmentState.AwaitingKardex, ManagementPlanStatus.InProgress);
            if (chain.Maintenance is { } maintenance)
            {
                if (maintenance.Status == MaintenanceStatus.Completed)
                    return (WizardPhase.Exit, WizardEquipmentState.AwaitingDeparture, ManagementPlanStatus.Completed);
                // Como Maintenances/Create: en curso (o con inicio) → InMaintenance; si no, espera L-8.
                var inProgress = maintenance.Status == MaintenanceStatus.InProgress || maintenance.StartDate != null;
                return (WizardPhase.Maintenance,
                    inProgress ? WizardEquipmentState.InMaintenance : WizardEquipmentState.AwaitingMaintenance,
                    ManagementPlanStatus.InProgress);
            }
            if (chain.TechnicalRequest != null)
                return (WizardPhase.Maintenance, WizardEquipmentState.AwaitingMaintenance, ManagementPlanStatus.InProgress);
            if (chain.Verification is { } verification)
                return verification.HasFailures
                    ? (WizardPhase.TechnicalRequest, WizardEquipmentState.AwaitingRequest, ManagementPlanStatus.InProgress)
                    : (WizardPhase.Verification, WizardEquipmentState.VerifiedGood, ManagementPlanStatus.InProgress);
            // Solo registros que no enlazan (p. ej. una salida que es préstamo): el plan queda pendiente de L-6.
            return (WizardPhase.Verification, WizardEquipmentState.PendingVerification, ManagementPlanStatus.Pending);
        }
    }
}
