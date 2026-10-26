using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    /// <summary>
    /// Punto único para cambiar el estado de un equipo (EquipmentUnit.CurrentStatus) manteniendo
    /// su historial como una línea de tiempo continua: la fila vigente (EndDate == null) se cierra
    /// en la fecha del cambio y se abre una nueva con el estado nuevo.
    /// No llama a SaveChanges: el llamador guarda dentro de su propia transacción.
    /// </summary>
    public interface IEquipmentStateHistoryService
    {
        /// <summary>
        /// Cambia el estado de la unidad (debe venir trackeada o ser nueva) y registra el tramo en el historial.
        /// Si el estado vigente ya es <paramref name="newStatus"/>, no duplica la fila y devuelve la vigente,
        /// salvo que <paramref name="splitIfSameStatus"/> sea true (p. ej. cambió solo la condición física y
        /// se quiere dejar constancia): entonces cierra el tramo y abre otro con el mismo estado.
        /// </summary>
        Task<EquipmentStateHistory> ChangeStatusAsync(EquipmentUnit unit, EquipmentStatus newStatus, string? reason, DateTime? at = null, int? userId = null, bool splitIfSameStatus = false);

        /// <summary>Crea la fila inicial (estado actual desde el alta) si la unidad no tiene historial.</summary>
        Task<EquipmentStateHistory> EnsureInitialAsync(EquipmentUnit unit, string reason, int? userId = null);
    }

    public class EquipmentStateHistoryService : IEquipmentStateHistoryService
    {
        /// <summary>Longitud máxima de Reason (nvarchar(500) en BD).</summary>
        public const int ReasonMaxLength = 500;

        private readonly ApplicationDbContext _context;

        public EquipmentStateHistoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EquipmentStateHistory> ChangeStatusAsync(EquipmentUnit unit, EquipmentStatus newStatus, string? reason, DateTime? at = null, int? userId = null, bool splitIfSameStatus = false)
        {
            ArgumentNullException.ThrowIfNull(unit);
            var when = at ?? DateTime.UtcNow;
            var previousStatus = unit.CurrentStatus;
            unit.CurrentStatus = newStatus;

            var open = await FindOpenAsync(unit);
            if (open == null)
            {
                var hasAny = await HasAnyHistoryAsync(unit);
                if (!hasAny)
                {
                    // Sin historial: la línea de tiempo empieza en el alta del equipo.
                    var origin = unit.CreatedDate == default || unit.CreatedDate > when ? when : unit.CreatedDate;
                    if (previousStatus == newStatus && !splitIfSameStatus)
                    {
                        var single = NewRow(unit, newStatus, origin, reason, userId);
                        _context.EquipmentStateHistories.Add(single);
                        return single;
                    }

                    var initial = NewRow(unit, previousStatus, origin, "Estado registrado desde el alta del equipo.", userId);
                    initial.EndDate = when < origin ? origin : when;
                    _context.EquipmentStateHistories.Add(initial);
                }

                var first = NewRow(unit, newStatus, when, reason, userId);
                _context.EquipmentStateHistories.Add(first);
                return first;
            }

            if (open.Status == newStatus && !splitIfSameStatus)
                return open;

            // EndDate nunca menor que StartDate (CK_EquipmentStateHistories_Dates).
            var closeAt = when < open.StartDate ? open.StartDate : when;
            open.EndDate = closeAt;

            // Datos heredados con varias filas abiertas: se cierran todas para dejar un solo tramo vigente.
            foreach (var extra in await FindOtherOpenAsync(unit, open))
                extra.EndDate = closeAt < extra.StartDate ? extra.StartDate : closeAt;

            var next = NewRow(unit, newStatus, closeAt, reason, userId);
            _context.EquipmentStateHistories.Add(next);
            return next;
        }

        public async Task<EquipmentStateHistory> EnsureInitialAsync(EquipmentUnit unit, string reason, int? userId = null)
        {
            ArgumentNullException.ThrowIfNull(unit);
            var open = await FindOpenAsync(unit);
            if (open != null)
                return open;

            if (await HasAnyHistoryAsync(unit))
            {
                // Tiene historial pero ninguna fila vigente: se abre el tramo actual.
                var latest = await LatestAsync(unit);
                var start = latest?.EndDate ?? DateTime.UtcNow;
                var reopened = NewRow(unit, unit.CurrentStatus, start, reason, userId);
                _context.EquipmentStateHistories.Add(reopened);
                return reopened;
            }

            var origin = unit.CreatedDate == default ? DateTime.UtcNow : unit.CreatedDate;
            var initial = NewRow(unit, unit.CurrentStatus, origin, reason, userId);
            _context.EquipmentStateHistories.Add(initial);
            return initial;
        }

        /// <summary>Recorta el motivo al máximo de la columna.</summary>
        public static string? TrimReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return null;
            return reason.Length <= ReasonMaxLength ? reason : reason[..ReasonMaxLength];
        }

        private EquipmentStateHistory NewRow(EquipmentUnit unit, EquipmentStatus status, DateTime start, string? reason, int? userId)
        {
            var row = new EquipmentStateHistory
            {
                Status = status,
                StartDate = start,
                EndDate = null,
                Reason = TrimReason(reason),
                CreatedById = userId,
                CreatedDate = DateTime.UtcNow
            };
            if (unit.Id > 0)
                row.EquipmentUnitId = unit.Id;
            else
                row.EquipmentUnit = unit; // unidad nueva: EF resuelve la FK al guardar
            return row;
        }

        /// <summary>Filas pendientes de guardar (Added) de esta unidad en el contexto actual.</summary>
        private IEnumerable<EquipmentStateHistory> PendingRows(EquipmentUnit unit) =>
            _context.ChangeTracker.Entries<EquipmentStateHistory>()
                .Where(e => e.State == EntityState.Added
                            && (ReferenceEquals(e.Entity.EquipmentUnit, unit) || (unit.Id > 0 && e.Entity.EquipmentUnitId == unit.Id)))
                .Select(e => e.Entity);

        private async Task<EquipmentStateHistory?> FindOpenAsync(EquipmentUnit unit)
        {
            var pending = PendingRows(unit).Where(h => h.EndDate == null)
                .OrderByDescending(h => h.StartDate).FirstOrDefault();
            if (pending != null) return pending;
            if (unit.Id <= 0) return null;

            // El filtro se repite en memoria: una fila trackeada pudo cerrarse en este mismo contexto sin guardarse aún.
            var stored = await _context.EquipmentStateHistories.AsTracking()
                .Where(h => h.EquipmentUnitId == unit.Id && h.EndDate == null)
                .ToListAsync();
            return stored.Where(h => h.EndDate == null)
                .OrderByDescending(h => h.StartDate).ThenByDescending(h => h.Id)
                .FirstOrDefault();
        }

        private async Task<List<EquipmentStateHistory>> FindOtherOpenAsync(EquipmentUnit unit, EquipmentStateHistory keep)
        {
            var result = PendingRows(unit).Where(h => h.EndDate == null && !ReferenceEquals(h, keep)).ToList();
            if (unit.Id > 0)
            {
                var keepId = keep.Id;
                var stored = await _context.EquipmentStateHistories.AsTracking()
                    .Where(h => h.EquipmentUnitId == unit.Id && h.EndDate == null && h.Id != keepId)
                    .ToListAsync();
                result.AddRange(stored.Where(h => h.EndDate == null && !ReferenceEquals(h, keep)));
            }
            return result;
        }

        private async Task<bool> HasAnyHistoryAsync(EquipmentUnit unit)
        {
            if (PendingRows(unit).Any()) return true;
            if (unit.Id <= 0) return false;
            return await _context.EquipmentStateHistories.AnyAsync(h => h.EquipmentUnitId == unit.Id);
        }

        private async Task<EquipmentStateHistory?> LatestAsync(EquipmentUnit unit)
        {
            if (unit.Id <= 0) return null;
            return await _context.EquipmentStateHistories
                .Where(h => h.EquipmentUnitId == unit.Id)
                .OrderByDescending(h => h.EndDate ?? h.StartDate).ThenByDescending(h => h.Id)
                .FirstOrDefaultAsync();
        }
    }
}
