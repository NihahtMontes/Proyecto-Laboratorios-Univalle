using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services.Notifications;

/// <summary>
/// Avisos persistidos de mantenimiento dirigidos al técnico responsable.
/// El aviso nunca forma parte del guardado crítico: cualquier fallo solo se registra.
/// </summary>
public sealed class MaintenanceNotificationWriter : IMaintenanceNotificationWriter
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MaintenanceNotificationWriter> _logger;

    public MaintenanceNotificationWriter(ApplicationDbContext context, ILogger<MaintenanceNotificationWriter> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyAssignmentAsync(int maintenanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var maintenance = await _context.Maintenances
                .AsNoTracking()
                .Include(m => m.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .FirstOrDefaultAsync(m => m.Id == maintenanceId, cancellationToken);
            if (maintenance == null) return;

            // Avisos de este mantenimiento: misma ruta, con o sin querystring.
            var actionPrefix = $"/Maintenances/Details/{maintenanceId}?";
            var exactUrl = $"/Maintenances/Details/{maintenanceId}";
            var existing = await _context.Notifications
                .AsTracking()
                .Where(n => n.Scope == NotificationScopes.Maintenance
                    && n.ActionUrl != null
                    && (n.ActionUrl.StartsWith(actionPrefix) || n.ActionUrl == exactUrl))
                .ToListAsync(cancellationToken);

            // Cerrado o cancelado: el aviso deja de ser relevante.
            if (maintenance.Status is MaintenanceStatus.Completed or MaintenanceStatus.Cancelled)
            {
                foreach (var notification in existing)
                {
                    notification.IsRead = true;
                }

                if (existing.Count > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return;
            }

            if (!maintenance.TechnicianId.HasValue || !maintenance.ScheduledDate.HasValue) return;

            // El destinatario es el usuario cuyo Email coincide con el del técnico.
            var technicianEmail = await _context.People
                .AsNoTracking()
                .Where(p => p.Id == maintenance.TechnicianId.Value)
                .Select(p => p.Email)
                .FirstOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(technicianEmail)) return;

            var recipientId = await _context.Users
                .AsNoTracking()
                .Where(u => u.Email != null && u.Email.ToLower() == technicianEmail.ToLower())
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (recipientId == null) return;

            var managementType = await _context.Managements
                .AsNoTracking()
                .Where(m => m.Id == maintenance.ManagementId)
                .Select(m => (ManagementType?)m.Type)
                .FirstOrDefaultAsync(cancellationToken);

            var actionUrl = $"/Maintenances/Details/{maintenanceId}?ManagementId={maintenance.ManagementId}&ManagementType={managementType}";
            var message = $"Unidad {maintenance.EquipmentUnit?.InventoryNumber ?? "S/N"} · {maintenance.EquipmentUnit?.Equipment?.Name}: programado para el {maintenance.ScheduledDate.Value:dd/MM/yyyy}.";

            var own = existing.FirstOrDefault(n => n.UserId == recipientId.Value);
            if (own != null)
            {
                if (own.Message == message) return;

                own.Message = message;
                own.ActionUrl = actionUrl;
                own.IsRead = false;
                own.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                // Cambió el técnico: los avisos del anterior dejan de estar vigentes.
                foreach (var notification in existing)
                {
                    notification.IsRead = true;
                }

                _context.Notifications.Add(new Notification
                {
                    UserId = recipientId.Value,
                    Title = "Mantenimiento asignado",
                    Message = message,
                    ActionUrl = actionUrl,
                    IconClass = "fas fa-user-cog text-info",
                    Scope = NotificationScopes.Maintenance,
                    ManagementId = maintenance.ManagementId,
                    ManagementType = managementType,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo registrar el aviso del mantenimiento {MaintenanceId}.", maintenanceId);
        }
    }
}
