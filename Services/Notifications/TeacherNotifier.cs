using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services.Notifications;

/// <summary>Aviso para el docente de una planificación (queda en su campana; solo dentro del sistema).</summary>
public sealed record TeacherNotice(int TeacherPersonId, int PlanningId, int ManagementId, string Title, string Message, string Icon);

/// <summary>
/// Agrega al contexto los avisos para las cuentas activas de cada docente. No guarda: el llamador hace SaveChanges
/// dentro de su propia transacción, así el aviso existe solo si la operación se confirma.
/// Un docente sin cuenta de usuario no recibe nada (no es un error).
/// </summary>
public static class TeacherNotifier
{
    public static async Task AddAsync(ApplicationDbContext context, IReadOnlyCollection<TeacherNotice> notices, CancellationToken ct = default)
    {
        if (notices.Count == 0) return;
        var personIds = notices.Select(n => n.TeacherPersonId).Distinct().ToList();
        var users = await context.Users.AsNoTracking()
            .Where(u => u.PersonId != null && personIds.Contains(u.PersonId.Value) && u.Status == GeneralStatus.Activo)
            .Select(u => new { u.Id, PersonId = u.PersonId!.Value })
            .ToListAsync(ct);
        foreach (var notice in notices)
        {
            foreach (var user in users.Where(u => u.PersonId == notice.TeacherPersonId))
            {
                context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = Cut(notice.Title, 100),
                    Message = Cut(notice.Message, 255),
                    ActionUrl = $"/Docente/Solicitud?id={notice.PlanningId}",
                    IconClass = notice.Icon,
                    ManagementId = notice.ManagementId,
                    Scope = NotificationScopes.Planning,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}
