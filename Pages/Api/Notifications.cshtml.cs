using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Api
{
    [Authorize]
    public class NotificationsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public NotificationsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<JsonResult> OnGetUnreadAsync(string? filter = null, int? managementId = null, string? managementType = null, string? scope = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return new JsonResult(new { count = 0, items = new List<object>() });

            var normalizedScope = scope?.Trim().ToLowerInvariant();
            ManagementType? parsedManagementType = null;
            if (!string.IsNullOrWhiteSpace(managementType) &&
                Enum.TryParse<ManagementType>(managementType.Trim(), ignoreCase: true, out var typeValue))
            {
                parsedManagementType = typeValue;
            }

            var query = _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == user.Id && !n.IsRead);

            query = ApplyScope(query, normalizedScope);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var term = filter.Trim().ToLowerInvariant();
                query = query.Where(n =>
                    n.Title.ToLower().Contains(term) ||
                    n.Message.ToLower().Contains(term) ||
                    (n.ActionUrl != null && n.ActionUrl.ToLower().Contains(term)) ||
                    (n.IconClass != null && n.IconClass.ToLower().Contains(term)));
            }

            if (managementId.HasValue)
            {
                var token = $"managementid={managementId.Value}";
                query = query.Where(n =>
                    n.ManagementId == managementId.Value ||
                    (n.ManagementId == null &&
                        (n.ActionUrl == null ||
                         !n.ActionUrl.ToLower().Contains("managementid=") ||
                         n.ActionUrl.ToLower().Contains(token))));
            }

            if (parsedManagementType.HasValue)
            {
                var typeToken = $"managementtype={parsedManagementType.Value.ToString().ToLower()}";
                query = query.Where(n =>
                    n.ManagementType == parsedManagementType.Value ||
                    (n.ManagementType == null &&
                        (n.ActionUrl == null ||
                         !n.ActionUrl.ToLower().Contains("managementtype=") ||
                         n.ActionUrl.ToLower().Contains(typeToken))));
            }

            var persistedCount = await query.CountAsync();

            var persisted = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new NotificationItem
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Url = n.ActionUrl,
                    Icon = n.IconClass,
                    SortDate = n.CreatedAt,
                    Time = n.CreatedAt.ToString("dd/MM HH:mm")
                })
                .ToListAsync();

            var syntheticCount = 0;
            var items = persisted;

            if (normalizedScope == "dashboard")
            {
                var anticipated = await BuildDashboardAnticipatedNotificationsAsync(managementId, parsedManagementType);
                syntheticCount = anticipated.Count;
                items = items
                    .Concat(anticipated)
                    .OrderBy(i => i.SortDate.Date)
                    .ThenByDescending(i => i.Id)
                    .Take(10)
                    .ToList();
            }

            return new JsonResult(new
            {
                count = persistedCount + syntheticCount,
                items = items.Select(i => new
                {
                    id = i.Id,
                    title = i.Title,
                    message = i.Message,
                    url = i.Url,
                    icon = i.Icon,
                    time = i.Time
                })
            });
        }

        public async Task<IActionResult> OnGetMarkAsReadAsync(int id)
        {
            if (id < 0)
                return new OkResult();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notif = await _context.Notifications.AsTracking().FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return new OkResult();
        }

        private static IQueryable<Notification> ApplyScope(IQueryable<Notification> query, string? scope)
        {
            if (scope == "maintenance")
            {
                return query.Where(n =>
                    n.Scope == "maintenance" ||
                    (n.Scope == null &&
                        (n.Title.ToLower().Contains("mantenimiento") ||
                         n.Message.ToLower().Contains("mantenimiento") ||
                         (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/maintenances")) ||
                         (n.IconClass != null && (n.IconClass.ToLower().Contains("wrench") || n.IconClass.ToLower().Contains("tools"))))));
            }

            if (scope == "acquisition")
            {
                return query.Where(n =>
                    n.Scope == "acquisition" ||
                    (n.Scope == null &&
                        (n.Title.ToLower().Contains("adquis") ||
                         n.Message.ToLower().Contains("adquis") ||
                         n.Title.ToLower().Contains("compra") ||
                         n.Message.ToLower().Contains("compra") ||
                         (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/acquisitions")) ||
                         (n.IconClass != null && (n.IconClass.ToLower().Contains("shopping") || n.IconClass.ToLower().Contains("cart"))))));
            }

            if (scope == "request")
            {
                return query.Where(n =>
                    n.Scope == "request" ||
                    (n.Scope == null &&
                        (n.Title.ToLower().Contains("solicitud") ||
                         n.Message.ToLower().Contains("solicitud") ||
                         (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/requests"))) &&
                        !n.Title.ToLower().Contains("adquis") &&
                        !n.Message.ToLower().Contains("adquis") &&
                        !n.Title.ToLower().Contains("compra") &&
                        !n.Message.ToLower().Contains("compra") &&
                        (n.IconClass == null || (!n.IconClass.ToLower().Contains("shopping") && !n.IconClass.ToLower().Contains("cart")))));
            }

            if (scope == "departure")
            {
                return query.Where(n =>
                    n.Scope == "departure" ||
                    (n.Scope == null &&
                        (n.Title.ToLower().Contains("préstamo") ||
                         n.Title.ToLower().Contains("prestamo") ||
                         n.Title.ToLower().Contains("salida") ||
                         n.Message.ToLower().Contains("préstamo") ||
                         n.Message.ToLower().Contains("prestamo") ||
                         n.Message.ToLower().Contains("salida") ||
                         (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/departures")))));
            }

            return scope switch
            {
                "maintenance" => query.Where(n =>
                    n.Title.ToLower().Contains("mantenimiento") ||
                    n.Message.ToLower().Contains("mantenimiento") ||
                    (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/maintenances")) ||
                    (n.IconClass != null && (n.IconClass.ToLower().Contains("wrench") || n.IconClass.ToLower().Contains("tools")))),

                "acquisition" => query.Where(n =>
                    n.Title.ToLower().Contains("adquis") ||
                    n.Message.ToLower().Contains("adquis") ||
                    n.Title.ToLower().Contains("compra") ||
                    n.Message.ToLower().Contains("compra") ||
                    (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/acquisitions")) ||
                    (n.IconClass != null && (n.IconClass.ToLower().Contains("shopping") || n.IconClass.ToLower().Contains("cart")))),

                "request" => query.Where(n =>
                    (n.Title.ToLower().Contains("solicitud") ||
                     n.Message.ToLower().Contains("solicitud") ||
                     (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/requests"))) &&
                    !n.Title.ToLower().Contains("adquis") &&
                    !n.Message.ToLower().Contains("adquis") &&
                    !n.Title.ToLower().Contains("compra") &&
                    !n.Message.ToLower().Contains("compra") &&
                    (n.IconClass == null || (!n.IconClass.ToLower().Contains("shopping") && !n.IconClass.ToLower().Contains("cart")))),

                "departure" => query.Where(n =>
                    n.Title.ToLower().Contains("préstamo") ||
                    n.Title.ToLower().Contains("prestamo") ||
                    n.Title.ToLower().Contains("salida") ||
                    n.Message.ToLower().Contains("préstamo") ||
                    n.Message.ToLower().Contains("prestamo") ||
                    n.Message.ToLower().Contains("salida") ||
                    (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/departures"))),

                _ => query
            };
        }

        private async Task<List<NotificationItem>> BuildDashboardAnticipatedNotificationsAsync(int? managementId, ManagementType? managementType)
        {
            var today = DateTime.UtcNow.Date;
            var items = new List<NotificationItem>();

            if (managementId.HasValue && !managementType.HasValue)
            {
                managementType = await _context.Managements
                    .AsNoTracking()
                    .Where(m => m.Id == managementId.Value)
                    .Select(m => (ManagementType?)m.Type)
                    .FirstOrDefaultAsync();
            }

            var typeQuery = managementType.HasValue ? $"&ManagementType={managementType.Value}" : string.Empty;
            var maintenanceTitle = managementType == ManagementType.Corrective
                ? "Mantenimiento correctivo próximo"
                : "Mantenimiento preventivo próximo";

            var maintenanceQuery = _context.Maintenances
                .AsNoTracking()
                .Include(m => m.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Where(m =>
                    m.ScheduledDate.HasValue &&
                    m.ScheduledDate.Value.Date >= today &&
                    m.ScheduledDate.Value.Date <= today.AddDays(7) &&
                    m.Status != MaintenanceStatus.Completed &&
                    m.Status != MaintenanceStatus.Cancelled);

            if (managementId.HasValue)
            {
                maintenanceQuery = maintenanceQuery.Where(m => m.ManagementId == managementId.Value);
            }

            var maintenances = await maintenanceQuery
                .OrderBy(m => m.ScheduledDate)
                .Take(8)
                .ToListAsync();

            items.AddRange(maintenances.Select(m =>
            {
                var scheduledDate = m.ScheduledDate!.Value.Date;
                var unit = m.EquipmentUnit?.InventoryNumber ?? "S/N";
                return new NotificationItem
                {
                    Id = -100000 - m.Id,
                    Title = maintenanceTitle,
                    Message = $"Unidad {unit}: programado para el {scheduledDate:dd/MM/yyyy}.",
                    Url = $"/Maintenances/Details/{m.Id}?ManagementId={m.ManagementId}{typeQuery}&Step=3",
                    Icon = "fas fa-tools text-info",
                    SortDate = scheduledDate,
                    Time = RelativeDayLabel(today, scheduledDate)
                };
            }));

            var departureQuery = _context.Departures
                .AsNoTracking()
                .Include(d => d.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Where(d =>
                    d.ActualReturnDate == null &&
                    d.Status != LoanStatus.Returned &&
                    d.Status != LoanStatus.Cancelled &&
                    d.EstimatedReturnDate.Date >= today &&
                    d.EstimatedReturnDate.Date <= today.AddDays(7));

            if (managementId.HasValue)
            {
                departureQuery = departureQuery.Where(d => d.ManagementId == managementId.Value);
            }

            var departures = await departureQuery
                .OrderBy(d => d.EstimatedReturnDate)
                .Take(8)
                .ToListAsync();

            items.AddRange(departures.Select(d =>
            {
                var returnDate = d.EstimatedReturnDate.Date;
                var unit = d.EquipmentUnit?.InventoryNumber ?? "S/N";
                return new NotificationItem
                {
                    Id = -200000 - d.Id,
                    Title = "Préstamo por vencer",
                    Message = $"Unidad {unit}: devolución estimada el {returnDate:dd/MM/yyyy}.",
                    Url = $"/Departures/Details?id={d.Id}&ManagementId={d.ManagementId}{typeQuery}&Step=4",
                    Icon = "fas fa-exchange-alt text-warning",
                    SortDate = returnDate,
                    Time = RelativeDayLabel(today, returnDate)
                };
            }));

            var acquisitionPlanQuery = _context.ManagementPlans
                .AsNoTracking()
                .Include(p => p.EquipmentUnit)
                .Include(p => p.AcquisitionRequest)
                .Where(p =>
                    p.AcquisitionRequestId.HasValue &&
                    p.AcquisitionRequest != null &&
                    p.AcquisitionRequest.Type == RequestType.Purchasing &&
                    p.AcquisitionRequest.Status == RequestStatus.Pending &&
                    p.PlannedDate.HasValue &&
                    p.PlannedDate.Value.Date >= today &&
                    p.PlannedDate.Value.Date <= today.AddDays(14));

            if (managementId.HasValue)
            {
                acquisitionPlanQuery = acquisitionPlanQuery.Where(p => p.ManagementId == managementId.Value);
            }

            var acquisitionPlans = await acquisitionPlanQuery
                .OrderBy(p => p.PlannedDate)
                .Take(8)
                .ToListAsync();

            items.AddRange(acquisitionPlans.Select(p =>
            {
                var dueDate = p.PlannedDate!.Value.Date;
                var request = p.AcquisitionRequest!;
                return new NotificationItem
                {
                    Id = -300000 - request.Id,
                    Title = "Adquisición pendiente",
                    Message = $"Solicitud #{request.Id}: revisar antes del {dueDate:dd/MM/yyyy}.",
                    Url = $"/Requests/Details/{request.Id}?IsWizard=true&ManagementId={p.ManagementId}{typeQuery}&Step=6",
                    Icon = "fas fa-shopping-cart text-success",
                    SortDate = dueDate,
                    Time = RelativeDayLabel(today, dueDate)
                };
            }));

            var plannedAcquisitionIds = acquisitionPlans.Select(p => p.AcquisitionRequestId!.Value).ToHashSet();
            var acquisitionQuery = _context.Requests
                .AsNoTracking()
                .Where(r =>
                    r.Type == RequestType.Purchasing &&
                    r.Status == RequestStatus.Pending &&
                    r.CreatedDate.Date <= today.AddDays(-14) &&
                    !plannedAcquisitionIds.Contains(r.Id));

            if (managementId.HasValue)
            {
                acquisitionQuery = acquisitionQuery.Where(r => r.ManagementId == managementId.Value);
            }

            var oldAcquisitions = await acquisitionQuery
                .OrderBy(r => r.CreatedDate)
                .Take(5)
                .ToListAsync();

            items.AddRange(oldAcquisitions.Select(r => new NotificationItem
            {
                Id = -400000 - r.Id,
                Title = "Adquisición requiere seguimiento",
                Message = $"Solicitud #{r.Id}: pendiente desde el {r.CreatedDate:dd/MM/yyyy}.",
                Url = $"/Requests/Details/{r.Id}?IsWizard=true&ManagementId={r.ManagementId}{typeQuery}&Step=6",
                Icon = "fas fa-shopping-cart text-success",
                SortDate = r.CreatedDate.Date,
                Time = "Más de 2 semanas"
            }));

            return items
                .GroupBy(i => i.Id)
                .Select(g => g.First())
                .OrderBy(i => i.SortDate.Date)
                .Take(20)
                .ToList();
        }

        private static string RelativeDayLabel(DateTime today, DateTime targetDate)
        {
            var days = (targetDate.Date - today.Date).Days;
            return days switch
            {
                < 0 => $"Vencido hace {Math.Abs(days)} día(s)",
                0 => "Hoy",
                1 => "Mañana",
                _ => $"En {days} días"
            };
        }

        private sealed class NotificationItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? Url { get; set; }
            public string? Icon { get; set; }
            public string Time { get; set; } = string.Empty;
            public DateTime SortDate { get; set; }
        }
    }
}
