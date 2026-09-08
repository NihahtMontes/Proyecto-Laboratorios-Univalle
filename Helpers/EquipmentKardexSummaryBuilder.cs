using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.Globalization;

namespace Proyecto_Laboratorios_Univalle.Helpers;

public sealed record EquipmentKardexSummary(
    int EquipmentUnitId,
    string Name,
    string InventoryNumber,
    string CurrentStatus,
    string CurrentStatusBadgeClass,
    EquipmentStateChangeSummary? LastStateChange,
    EquipmentMaintenanceSummary? LastMaintenance);

public sealed record EquipmentStateChangeSummary(
    int Id,
    string Status,
    string BadgeClass,
    string StartDate,
    string? EndDate,
    string Reason,
    string RegisteredBy);

public sealed record EquipmentMaintenanceSummary(
    int Id,
    string Date,
    string Type,
    string ServiceType,
    string Status,
    string BadgeClass,
    string Management,
    string Technician,
    string Description,
    int TaskCount,
    int CostItemCount,
    string TotalCost);

public static class EquipmentKardexSummaryBuilder
{
    private static readonly CultureInfo SpanishCulture = CultureInfo.GetCultureInfo("es-BO");

    public static async Task<EquipmentKardexSummary?> BuildAsync(
        ApplicationDbContext context,
        int equipmentUnitId,
        CancellationToken cancellationToken = default)
    {
        var unit = await context.EquipmentUnits
            .AsNoTracking()
            .Include(u => u.Equipment)
            .FirstOrDefaultAsync(u => u.Id == equipmentUnitId, cancellationToken);

        if (unit == null) return null;

        var lastState = await context.EquipmentStateHistories
            .AsNoTracking()
            .Include(h => h.CreatedBy)
            .Where(h => h.EquipmentUnitId == equipmentUnitId)
            .OrderByDescending(h => h.StartDate)
            .ThenByDescending(h => h.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var lastMaintenance = await context.Maintenances
            .AsNoTracking()
            .Include(m => m.Management)
            .Include(m => m.Technician)
            .Include(m => m.Tasks)
            .Include(m => m.CostDetails)
            .Where(m => m.EquipmentUnitId == equipmentUnitId)
            .OrderByDescending(m => m.EndDate ?? m.StartDate ?? m.ScheduledDate ?? m.CreatedDate)
            .ThenByDescending(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new EquipmentKardexSummary(
            unit.Id,
            unit.Equipment?.Name ?? "Sin nombre",
            unit.InventoryNumber,
            EnumHelper.GetDisplayName(unit.CurrentStatus),
            GetEquipmentStatusBadge(unit.CurrentStatus),
            lastState == null
                ? null
                : new EquipmentStateChangeSummary(
                    lastState.Id,
                    EnumHelper.GetDisplayName(lastState.Status),
                    GetEquipmentStatusBadge(lastState.Status),
                    FormatDate(lastState.StartDate),
                    lastState.EndDate.HasValue ? FormatDate(lastState.EndDate.Value) : null,
                    string.IsNullOrWhiteSpace(lastState.Reason) ? "Sin motivo registrado" : lastState.Reason,
                    lastState.CreatedBy?.FullName ?? "Sistema"),
            lastMaintenance == null
                ? null
                : new EquipmentMaintenanceSummary(
                    lastMaintenance.Id,
                    FormatDate(lastMaintenance.EndDate ?? lastMaintenance.StartDate ?? lastMaintenance.ScheduledDate ?? lastMaintenance.CreatedDate),
                    EnumHelper.GetDisplayName(lastMaintenance.MaintenanceType),
                    lastMaintenance.ServiceType == ServiceType.External ? "Externo" : "Interno",
                    EnumHelper.GetDisplayName(lastMaintenance.Status),
                    GetMaintenanceStatusBadge(lastMaintenance.Status),
                    FormatManagement(lastMaintenance.Management),
                    lastMaintenance.Technician?.FullName ?? "Sin técnico asignado",
                    string.IsNullOrWhiteSpace(lastMaintenance.Description) ? "Sin descripción registrada" : lastMaintenance.Description,
                    lastMaintenance.Tasks.Count(task => !task.IsDeleted),
                    lastMaintenance.CostDetails.Count,
                    FormatMoney(lastMaintenance.ActualCost ?? lastMaintenance.CalculatedTotal)));
    }

    private static string FormatDate(DateTime value) =>
        value.ToString("dd 'de' MMMM, yyyy", SpanishCulture);

    private static string FormatMoney(decimal value) =>
        value > 0 ? $"Bs {value:N2}" : "Sin costo registrado";

    private static string FormatManagement(Management? management)
    {
        if (management == null) return "Gestión sin detalle";
        if (!string.IsNullOrWhiteSpace(management.Code)) return management.Code;
        return management.Type == ManagementType.Corrective
            ? $"CORR-{management.Year}-{management.Semester}"
            : $"{management.Year}-{management.Semester}";
    }

    private static string GetEquipmentStatusBadge(EquipmentStatus status) => status switch
    {
        EquipmentStatus.Operational => "badge-success",
        EquipmentStatus.UnderMaintenance or EquipmentStatus.InRepair => "badge-warning",
        EquipmentStatus.OutOfService or EquipmentStatus.Broken => "badge-danger",
        EquipmentStatus.OnLoan => "badge-info",
        _ => "badge-secondary"
    };

    private static string GetMaintenanceStatusBadge(MaintenanceStatus status) => status switch
    {
        MaintenanceStatus.Completed => "badge-success",
        MaintenanceStatus.InProgress => "badge-warning",
        MaintenanceStatus.Scheduled => "badge-info",
        MaintenanceStatus.Pending => "badge-secondary",
        MaintenanceStatus.Cancelled => "badge-danger",
        _ => "badge-secondary"
    };
}
