using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.Notifications;

namespace Proyecto_Laboratorios_Univalle.Pages.Notifications
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly INotificationAlertService _alertService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            INotificationAlertService alertService,
            UserManager<User> userManager,
            ILogger<IndexModel> logger)
        {
            _alertService = alertService;
            _userManager = userManager;
            _logger = logger;
        }

        public NotificationFeed Feed { get; private set; } =
            new NotificationFeed(0, 0, new Dictionary<string, int>(), Array.Empty<NotificationItem>());

        public IReadOnlyList<NotificationItem> PendingItems { get; private set; } = Array.Empty<NotificationItem>();

        public IReadOnlyList<NotificationItem> History { get; private set; } = Array.Empty<NotificationItem>();

        public string? Domain { get; private set; }

        public int KpiPending { get; private set; }

        public int KpiCritical { get; private set; }

        public int KpiOverdue { get; private set; }

        public int KpiToday { get; private set; }

        public async Task OnGetAsync(string? domain)
        {
            Domain = NormalizeDomain(domain);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            Feed = await _alertService.GetFeedAsync(new NotificationQuery(
                user.Id,
                DateTime.Today,
                NotificationScopes.All,
                null,
                null,
                NotificationAudience.FromPrincipal(User),
                Take: 200));

            History = await _alertService.GetHistoryAsync(user.Id, 100);

            PendingItems = string.IsNullOrEmpty(Domain)
                ? Feed.Items
                : Feed.Items.Where(i => i.Domain.Equals(Domain, StringComparison.OrdinalIgnoreCase)).ToList();

            var filteredHistory = string.IsNullOrEmpty(Domain)
                ? History
                : History.Where(i => i.Domain.Equals(Domain, StringComparison.OrdinalIgnoreCase)).ToList();
            History = filteredHistory;

            if (string.IsNullOrEmpty(Domain))
            {
                KpiPending = Feed.Count;
                KpiCritical = Feed.CriticalCount;
                KpiOverdue = Feed.GroupCounts.TryGetValue(NotificationGroups.Overdue, out var overdue) ? overdue : 0;
                KpiToday = Feed.GroupCounts.TryGetValue(NotificationGroups.Today, out var today) ? today : 0;
            }
            else
            {
                KpiPending = PendingItems.Count;
                KpiCritical = PendingItems.Count(i =>
                    i.Urgency == NotificationUrgency.Danger || i.Urgency == NotificationUrgency.Warning);
                KpiOverdue = PendingItems.Count(i => i.Group == NotificationGroups.Overdue);
                KpiToday = PendingItems.Count(i => i.Group == NotificationGroups.Today);
            }
        }

        public async Task<IActionResult> OnPostMarkAsReadAsync(int id, string? domain)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (id > 0)
            {
                try
                {
                    await _alertService.MarkAsReadAsync(user.Id, id);
                    TempData.Success("Notificación marcada como leída.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo marcar como leída la notificación {NotificationId}.", id);
                    TempData.Error("No se pudo marcar la notificación. Intente nuevamente.");
                }
            }

            return RedirectToPage(new { domain = NormalizeDomain(domain) });
        }

        public async Task<IActionResult> OnPostMarkAllAsReadAsync(string? domain)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            try
            {
                await _alertService.MarkAllAsReadAsync(user.Id);
                TempData.Success("Todas las notificaciones fueron marcadas como leídas.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudieron marcar todas las notificaciones como leídas para el usuario {UserId}.", user.Id);
                TempData.Error("No se pudieron marcar todas las notificaciones. Intente nuevamente.");
            }

            return RedirectToPage(new { domain = NormalizeDomain(domain) });
        }

        private static string? NormalizeDomain(string? domain) =>
            domain?.Trim().ToLowerInvariant() is string d && d is
                NotificationDomains.Maintenance or
                NotificationDomains.Planning or
                NotificationDomains.Departure or
                NotificationDomains.Acquisition or
                NotificationDomains.General
                ? d
                : null;
    }
}
