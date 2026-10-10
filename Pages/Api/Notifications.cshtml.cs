using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services.Notifications;

namespace Proyecto_Laboratorios_Univalle.Pages.Api
{
    /// <summary>
    /// API de la campana de notificaciones: delega en INotificationAlertService (fuente única de avisos).
    /// El marcado de lectura es POST (el cliente envía el token antifalsificación).
    /// </summary>
    [Authorize]
    public class NotificationsModel : PageModel
    {
        private readonly INotificationAlertService _alerts;
        private readonly UserManager<User> _userManager;

        public NotificationsModel(INotificationAlertService alerts, UserManager<User> userManager)
        {
            _alerts = alerts;
            _userManager = userManager;
        }

        public async Task<JsonResult> OnGetUnreadAsync(string? filter = null, int? managementId = null, string? managementType = null, string? scope = null, int? take = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new JsonResult(new
                {
                    count = 0,
                    criticalCount = 0,
                    groupCounts = new Dictionary<string, int>(),
                    items = Array.Empty<object>()
                });
            }

            ManagementType? parsedManagementType = null;
            if (!string.IsNullOrWhiteSpace(managementType) &&
                Enum.TryParse<ManagementType>(managementType.Trim(), ignoreCase: true, out var typeValue))
            {
                parsedManagementType = typeValue;
            }

            var effectiveTake = take is null or < 1 ? 30 : Math.Min(take.Value, 100);

            var query = new NotificationQuery(
                UserId: user.Id,
                Today: DateTime.Today,
                Scope: scope,
                ManagementId: managementId,
                ManagementType: parsedManagementType,
                Audience: NotificationAudience.FromPrincipal(User),
                Filter: filter,
                Take: effectiveTake);

            var feed = await _alerts.GetFeedAsync(query);

            return new JsonResult(new
            {
                count = feed.Count,
                criticalCount = feed.CriticalCount,
                groupCounts = feed.GroupCounts,
                items = feed.Items.Select(i => new
                {
                    id = i.Id,
                    title = i.Title,
                    message = i.Message,
                    url = i.Url,
                    icon = i.Icon,
                    time = i.Time,
                    syntheticKey = i.SyntheticKey,
                    group = i.Group,
                    urgency = i.Urgency,
                    domain = i.Domain,
                    isPersisted = i.IsPersisted
                })
            });
        }

        public async Task<JsonResult> OnPostMarkAsReadAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new JsonResult(new { ok = false });
            }

            await _alerts.MarkAsReadAsync(user.Id, id);
            return new JsonResult(new { ok = true });
        }

        public async Task<JsonResult> OnPostMarkAllAsReadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new JsonResult(new { ok = false });
            }

            await _alerts.MarkAllAsReadAsync(user.Id);
            return new JsonResult(new { ok = true });
        }
    }
}
