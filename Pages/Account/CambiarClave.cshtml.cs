using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Account;

/// <summary>
/// Cambio de contraseña propio. Obligatorio en el primer ingreso cuando la contraseña la generó el sistema
/// (User.MustChangePassword); el middleware TeacherAccess no deja usar la aplicación hasta cambiarla.
/// </summary>
[Authorize]
public class CambiarClaveModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public CambiarClaveModel(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public bool IsMandatory { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Escribe tu contraseña actual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe la nueva contraseña.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "La nueva contraseña debe tener al menos 12 caracteres.")]
        [Display(Name = "Nueva contraseña")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repite la nueva contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Repetir nueva contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet() => IsMandatory = TeacherAccess.MustChangePassword(User);

    public async Task<IActionResult> OnPostAsync()
    {
        IsMandatory = TeacherAccess.MustChangePassword(User);
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Login");
        if (Input.NewPassword == Input.CurrentPassword)
        {
            ModelState.AddModelError("Input.NewPassword", "La nueva contraseña debe ser distinta de la actual.");
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code == "PasswordMismatch" ? "Input.CurrentPassword" : "Input.NewPassword",
                    error.Code == "PasswordMismatch" ? "La contraseña actual no es correcta." : error.Description);
            return Page();
        }

        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
        }
        // Renueva la cookie para que desaparezca la marca de cambio obligatorio.
        await _signInManager.RefreshSignInAsync(user);
        TempData.Success("Contraseña actualizada correctamente.");
        return LocalRedirect(TeacherAccess.IsTeacherOnly(User) || user.Role == Models.Enums.UserRole.Docente ? "/Docente" : "/");
    }
}
