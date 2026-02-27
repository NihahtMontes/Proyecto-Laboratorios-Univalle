using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Persons
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public class InputModel
        {
            [Required(ErrorMessage = "El tipo de registro es obligatorio")]
            public bool IsInternal { get; set; }

            [Required(ErrorMessage = "El nombre / razón social es obligatorio")]
            [StringLength(200)]
            [RegularExpression(@"^[a-zA-Z0-9\s.\-]*$", ErrorMessage = "Formato de nombre inválido")]
            public string Name { get; set; } = string.Empty;

            [EmailAddress(ErrorMessage = "Email inválido")]
            public string? Email { get; set; }

            [Phone(ErrorMessage = "Teléfono inválido")]
            public string? PhoneNumber { get; set; }

            // Extern fields
            public bool IsEntity { get; set; }
            public string? Address { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Person newPerson;

            if (Input.IsInternal)
            {
                newPerson = new Intern
                {
                    Name = Input.Name.Clean(),
                    InternStatus = GeneralStatus.Activo
                };
            }
            else
            {
                if (string.IsNullOrEmpty(Input.Address))
                {
                    ModelState.AddModelError("Input.Address", "La dirección es obligatoria para externos.");
                    return Page();
                }

                newPerson = new Extern
                {
                    Name = Input.Name.Clean(),
                    Address = Input.Address.Trim(),
                    IsEntity = Input.IsEntity,
                    ExternStatus = GeneralStatus.Activo
                };
            }

            // Common fields
            newPerson.Email = Input.Email?.Trim().ToLower();
            newPerson.PhoneNumber = Input.PhoneNumber?.Trim();
            newPerson.Status = GeneralStatus.Activo;

            try
            {
                _context.People.Add(newPerson);
                await _context.SaveChangesAsync();
                TempData.Success($"Registro de '{newPerson.FullName}' completado exitosamente.");
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData.Error($"Error al registrar: {ex.Message}");
                return Page();
            }
        }
    }
}
