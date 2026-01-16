        using Microsoft.AspNetCore.Authentication;
        using Microsoft.AspNetCore.Authorization;
        using Microsoft.AspNetCore.Identity;
        using Microsoft.AspNetCore.Mvc;
        using Microsoft.AspNetCore.Mvc.RazorPages;
        using Proyecto_Laboratorios_Univalle.Models;
        using System.ComponentModel.DataAnnotations;
        using System.Threading.Tasks;

        namespace Proyecto_Laboratorios_Univalle.Pages
        {
            [AllowAnonymous]
            public class LoginModel : PageModel
            {
                private readonly SignInManager<User> _signInManager;

                public LoginModel(SignInManager<User> signInManager)
                {
                    _signInManager = signInManager;
                }

                [BindProperty]
                public InputModel Input { get; set; } = new InputModel();

                public string? ReturnUrl { get; set; }
                public string? ErrorMessage { get; set; }

                public class InputModel
                {
                    [Required]
                    public string UserName { get; set; } = string.Empty;

                    [Required]
                    [DataType(DataType.Password)]
                    public string Password { get; set; } = string.Empty;

                    [Display(Name = "Remember me?")]
                    public bool RememberMe { get; set; }
                    // public string IdentityCard { get; internal set; } // Removed unused required field
                }

                public async Task OnGetAsync(string? returnUrl = null)
                {
                    if (!string.IsNullOrEmpty(ErrorMessage))
                    {
                        ModelState.AddModelError(string.Empty, ErrorMessage);
                    }

                    returnUrl ??= Url.Content("~/");

                    // Clear the existing external cookie to ensure a clean login process
                    await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                    ReturnUrl = returnUrl;
                }

                public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> DEBUG: OnPostAsync HIT!");
                    returnUrl ??= Url.Content("~/");

                    if (ModelState.IsValid)
                    {
                        // This doesn't count login failures towards account lockout
                        // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                        var result = await _signInManager.PasswordSignInAsync(Input.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                        // DEBUG: Log login result
                        System.Diagnostics.Debug.WriteLine($"Login Attempt for '{Input.UserName}': {result}");
                        if (!result.Succeeded)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failure Details -> IsLockedOut: {result.IsLockedOut}, IsNotAllowed: {result.IsNotAllowed}, RequiresTwoFactor: {result.RequiresTwoFactor}");
                        }

                        if (result.Succeeded)
                        {
                            System.Diagnostics.Debug.WriteLine(">>> LOGIN SUCCESSFUL! Redirecting...");
                            return LocalRedirect(returnUrl);
                        }
                        if (result.RequiresTwoFactor)
                        {
                            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                        }
                        if (result.IsLockedOut)
                        {
                            return RedirectToPage("./Lockout");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                            return Page();
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(">>> MODEL STATE IS INVALID! Errors:");
                        foreach (var modelState in ModelState.Values)
                        {
                            foreach (var error in modelState.Errors)
                            {
                                System.Diagnostics.Debug.WriteLine($"---> Error: {error.ErrorMessage} / {error.Exception}");
                            }
                        }
                    }

                    // If we got this far, something failed, redisplay form
                    return Page();
                }
            }
        }
