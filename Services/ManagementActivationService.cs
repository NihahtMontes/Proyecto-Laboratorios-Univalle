using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public interface IManagementActivationService
    {
        Task<ManagementActivationResult> ActivateAsync(Management management);
        Task<ManagementActivationResult> ReconcileActiveAsync(ManagementType type, int? preferredManagementId = null);
    }

    public class ManagementActivationResult
    {
        public Management? ActiveManagement { get; set; }
        public int ClosedCount { get; set; }
    }

    public class ManagementActivationService : IManagementActivationService
    {
        private readonly ApplicationDbContext _context;

        public ManagementActivationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ManagementActivationResult> ActivateAsync(Management management)
        {
            management.Status = ManagementStatus.Active;
            management.ActualClosedDate = null;

            var activeOthers = await _context.Managements
                .AsTracking()
                .Where(m => m.Type == management.Type
                    && m.Status == ManagementStatus.Active
                    && m.Id != management.Id)
                .ToListAsync();

            CloseManagements(activeOthers);

            return new ManagementActivationResult
            {
                ActiveManagement = management,
                ClosedCount = activeOthers.Count
            };
        }

        public async Task<ManagementActivationResult> ReconcileActiveAsync(ManagementType type, int? preferredManagementId = null)
        {
            var activeManagements = await _context.Managements
                .AsTracking()
                .Where(m => m.Type == type && m.Status == ManagementStatus.Active)
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Semester)
                .ThenByDescending(m => m.CreatedDate)
                .ToListAsync();

            if (!activeManagements.Any())
            {
                return new ManagementActivationResult();
            }

            var selected = preferredManagementId.HasValue
                ? activeManagements.FirstOrDefault(m => m.Id == preferredManagementId.Value) ?? activeManagements.First()
                : activeManagements.First();

            var toClose = activeManagements
                .Where(m => m.Id != selected.Id)
                .ToList();

            CloseManagements(toClose);

            return new ManagementActivationResult
            {
                ActiveManagement = selected,
                ClosedCount = toClose.Count
            };
        }

        private static void CloseManagements(IEnumerable<Management> managements)
        {
            var now = DateTime.UtcNow;

            foreach (var management in managements)
            {
                management.Status = ManagementStatus.Completed;
                management.ActualClosedDate ??= now;
                management.LastModifiedDate = now;
            }
        }
    }
}
