using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.Data;

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DeleteModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<DeleteModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// User to be deleted/revoked. 
        /// Named 'AppUser' to avoid collision with 'PageModel.User' (ClaimsPrincipal).
        /// </summary>
        [BindProperty]
        public User AppUser { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Retrieve the user including audit details
            var user = await _context.Users
                .Include(u => u.CreatedBy)
                .Include(u => u.ModifiedBy)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Role == UserRole.SuperAdmin &&
                !base.User.IsInRole(AuthorizationHelper.RoleSuperAdmin))
            {
                return Forbid();
            }
            
            AppUser = user;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Find the user in the database
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(base.User);
            if (currentUser?.Id == user.Id)
            {
                TempData.Error("No puede dar de baja su propia cuenta.");
                return RedirectToPage("./Index");
            }

            if (user.Role == UserRole.SuperAdmin &&
                !base.User.IsInRole(AuthorizationHelper.RoleSuperAdmin))
            {
                return Forbid();
            }

            if (user.Role == UserRole.SuperAdmin)
            {
                var hasAnotherActiveSuperAdmin = await _context.Users
                    .IgnoreQueryFilters()
                    .AnyAsync(candidate => candidate.Id != user.Id &&
                        candidate.Role == UserRole.SuperAdmin &&
                        candidate.Status == GeneralStatus.Activo);

                if (!hasAnotherActiveSuperAdmin)
                {
                    TempData.Error("Debe existir al menos un superadministrador activo.");
                    return RedirectToPage("./Index");
                }
            }

            // Check if the user is already deleted
            if (user.Status == GeneralStatus.Eliminado)
            {
                TempData.Warning($"El usuario '{user.FullName}' ya se encuentra dado de baja.");
                return RedirectToPage("./Index");
            }

            // Perform Soft Delete (Logic Delete for Audit)
            user.Status = GeneralStatus.Eliminado;
            user.LastModifiedDate = DateTime.UtcNow;

            if (currentUser != null)
            {
                user.ModifiedById = currentUser.Id;
            }

            try
            {
                // Serializable evita que dos bajas concurrentes eliminen los últimos SuperAdmin.
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                // Re-verificación dentro de la transacción contra estado vigente de DB.
                var liveTarget = await _context.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(u => u.Id == id)
                    .Select(u => new { u.Role, u.Status })
                    .FirstOrDefaultAsync();

                if (liveTarget?.Status == GeneralStatus.Eliminado)
                {
                    TempData.Warning($"El usuario '{user.FullName}' ya se encuentra dado de baja.");
                    return RedirectToPage("./Index");
                }

                if (liveTarget?.Role == UserRole.SuperAdmin)
                {
                    var hasAnotherActiveSuperAdmin = await _context.Users
                        .IgnoreQueryFilters()
                        .AnyAsync(candidate => candidate.Id != user.Id &&
                            candidate.Role == UserRole.SuperAdmin &&
                            candidate.Status == GeneralStatus.Activo);

                    if (!hasAnotherActiveSuperAdmin)
                    {
                        TempData.Error("Debe existir al menos un superadministrador activo.");
                        return RedirectToPage("./Index");
                    }
                }

                var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                if (!lockoutResult.Succeeded)
                {
                    TempData.Error("No se pudo revocar el acceso del usuario.");
                    return RedirectToPage("./Delete", new { id });
                }

                var stampResult = await _userManager.UpdateSecurityStampAsync(user);
                if (!stampResult.Succeeded)
                {
                    TempData.Error("No se pudo invalidar la sesion del usuario.");
                    return RedirectToPage("./Delete", new { id });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData.Success($"El acceso para '{user.FullName}' ha sido revocado correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo dar de baja al usuario {UserId}.", id);
                TempData.Error("No se pudo procesar la baja del usuario. Intente nuevamente.");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}
