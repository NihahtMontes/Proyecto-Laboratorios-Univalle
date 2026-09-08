using Microsoft.AspNetCore.Identity;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    public static class IdentityRoleExtensions
    {
        public static async Task<IdentityResult> SynchronizeManagedRoleAsync(
            this UserManager<User> userManager,
            User user,
            UserRole role)
        {
            var targetRole = AuthorizationHelper.ToIdentityRole(role);
            var currentRoles = await userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles
                .Where(current => AuthorizationHelper.ManagedIdentityRoles.Contains(current, StringComparer.OrdinalIgnoreCase)
                    && !string.Equals(current, targetRole, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (rolesToRemove.Length > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    return removeResult;
                }
            }

            if (!await userManager.IsInRoleAsync(user, targetRole))
            {
                return await userManager.AddToRoleAsync(user, targetRole);
            }

            return IdentityResult.Success;
        }
    }
}
