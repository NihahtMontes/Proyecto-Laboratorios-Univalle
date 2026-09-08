using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using System.Security.Claims;

namespace Proyecto_Laboratorios_Univalle.Services
{
    /// <summary>
    /// Custom Claims Principal Factory that automatically adds the user's role from the User.Role enum property
    /// as a Role claim for authorization purposes.
    /// Updated to match IdentityRole<int> configuration.
    /// </summary>
    public class UserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, IdentityRole<int>>
    {
        public UserClaimsPrincipalFactory(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        /// <summary>
        /// Creates a ClaimsPrincipal for the user, including the custom Role claim from User.Role enum
        /// </summary>
        public override async Task<ClaimsPrincipal> CreateAsync(User user)
        {
            var principal = await base.CreateAsync(user);
            var identity = (ClaimsIdentity?)principal.Identity ?? null;

            // El enum de dominio es la fuente de verdad para los roles administrados.
            // Se retiran claims Identity heredados que pudieran estar desincronizados.
            if (identity != null)
            {
                var staleManagedClaims = identity.FindAll(ClaimTypes.Role)
                    .Where(claim => AuthorizationHelper.ManagedIdentityRoles.Contains(
                        claim.Value,
                        StringComparer.OrdinalIgnoreCase))
                    .ToList();

                foreach (var claim in staleManagedClaims)
                {
                    identity.RemoveClaim(claim);
                }

                identity.AddClaim(new Claim(
                    ClaimTypes.Role,
                    AuthorizationHelper.ToIdentityRole(user.Role)));
            }

            return principal;
        }
    }
}
