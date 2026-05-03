using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;

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

        // ====================================================================
        // CAMBIO: Se añade el parámetro opcional "filter" para búsquedas específicas
        // ====================================================================
        public async Task<JsonResult> OnGetUnreadAsync(string? filter = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return new JsonResult(new { count = 0, items = new List<object>() });

            // 1. Iniciamos la consulta base (solo las de este usuario que no estén leídas)
            var query = _context.Notifications.Where(n => n.UserId == user.Id && !n.IsRead);

            // 2. Si la vista nos envía un filtro, buscamos esa palabra en el Título o URL
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(n => n.Title.Contains(filter) || (n.ActionUrl != null && n.ActionUrl.Contains(filter)));
            }

            // 3. Ejecutamos la consulta final
            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(10) // Mostrar solo las últimas 10
                .Select(n => new {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    url = n.ActionUrl,
                    icon = n.IconClass,
                    time = n.CreatedAt.ToString("dd/MM HH:mm")
                })
                .ToListAsync();

            return new JsonResult(new { count = notifications.Count, items = notifications });
        }

        // Este método marca una notificación como leída cuando le haces clic
        public async Task<IActionResult> OnGetMarkAsReadAsync(int id)
        {
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
    }
}