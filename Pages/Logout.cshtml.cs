using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Models;
using Microsoft.AspNetCore.Authentication;

namespace Proyecto_Laboratorios_Univalle.Pages
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;

        public LogoutModel(SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost(string ?returnUrl = null)
        {
            // 1. Standard Identity SignOut
            await _signInManager.SignOutAsync();

            // 2. Force Explicit Cookie Clearing (Fallback)
            // This ensures that even if Identity scheme matches fail, standard cookie is nuked
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            
            // Note: Session.Clear() removed as Session middleware is not active.

            // 3. Redirect
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // Explicitly valid redirect to Login page
                return RedirectToPage("/Login");
            }
        }
    }
}
