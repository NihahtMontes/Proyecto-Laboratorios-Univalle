using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Services
{
    /// <summary>
    /// Custom Claims Principal Factory that automatically adds the user's role from the User.Role enum property
    /// as a Role claim for authorization purposes.
    /// </summary>
    public class UserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User>
    {
        public UserClaimsPrincipalFactory(
            UserManager<User> userManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, optionsAccessor)
        {
        }

        /// <summary>
        /// Creates a ClaimsPrincipal for the user, including the custom Role claim from User.Role enum
        /// </summary>
        public override async Task<ClaimsPrincipal> CreateAsync(User user)
        {
            var principal = await base.CreateAsync(user);
            var identity = (ClaimsIdentity)principal.Identity;
            
            // Add the role claim from the User.Role enum property
            // This allows [Authorize(Roles = "...")] to work correctly
            if (identity != null)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
            }
            
            return principal;
        }
    }
}
