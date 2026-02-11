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

        [BindProperty]
        public Person Person { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Normalization
            Person.FirstName = Person.FirstName.Clean();
            Person.LastName = Person.LastName.Clean();
            Person.IdentityCard = Person.IdentityCard.Trim().ToUpper();
            Person.Email = Person.Email?.Trim().ToLower();

            // Duplicate CI Check
            bool ciExists = await _context.People
                .AnyAsync(p => p.IdentityCard == Person.IdentityCard && p.Status != GeneralStatus.Eliminado);

            if (ciExists)
            {
                ModelState.AddModelError("Person.IdentityCard", "Este documento de identidad ya se encuentra registrado.");
                return Page();
            }

            // Initialization
            Person.Status = GeneralStatus.Activo;
            Person.CreatedDate = DateTime.Now;

            // Auditing
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                Person.CreatedById = currentUser.Id;
            }

            try
            {
                _context.People.Add(Person);
                await _context.SaveChangesAsync();
                TempData.Success($"Identidad de '{Person.FullName}' registrada exitosamente.");
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData.Error($"Error al registrar la persona: {ex.Message}");
                return Page();
            }
        }
    }
}
