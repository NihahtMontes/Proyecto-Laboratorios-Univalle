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

        public async Task<IActionResult> OnGet()
        {
            return await PerformLogout();
        }

        public async Task<IActionResult> OnPost(string? returnUrl = null)
        {
            return await PerformLogout(returnUrl);
        }

        private async Task<IActionResult> PerformLogout(string? returnUrl = null)
        {
            // 1. Standard Identity SignOut
            await _signInManager.SignOutAsync();

            // 2. Force Explicit Cookie Clearing (Fallback)
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            // 3. Redirect back to Login if no returnUrl
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage("/Login");
            }
        }
    }
}
