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
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class EditInputModel
        {
            public int Id { get; set; }
            public bool IsInternal { get; set; }

            [Required(ErrorMessage = "El nombre / razón social es obligatorio")]
            [StringLength(200)]
            [RegularExpression(@"^[a-zA-Z0-9\s.\-]*$", ErrorMessage = "Formato de nombre inválido")]
            public string Name { get; set; } = string.Empty;

            [EmailAddress(ErrorMessage = "Email inválido")]
            public string? Email { get; set; }

            [Phone(ErrorMessage = "Teléfono inválido")]
            public string? PhoneNumber { get; set; }

            public GeneralStatus Status { get; set; }

            // Intern
            public GeneralStatus? InternStatus { get; set; }

            // Extern
            public bool IsEntity { get; set; }
            public string? Address { get; set; }
            public GeneralStatus? ExternStatus { get; set; }
        }

        [BindProperty]
        public EditInputModel Input { get; set; } = new();

        public Person Person { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.People
                .FirstOrDefaultAsync(m => m.Id == id);

            if (person == null)
            {
                return NotFound();
            }

            Person = person;
            Input = new EditInputModel
            {
                Id = person.Id,
                Name = person.FullName,
                Email = person.Email,
                PhoneNumber = person.PhoneNumber,
                Status = person.Status,
                IsInternal = person is Intern
            };

            if (person is Intern i)
            {
                Input.InternStatus = i.InternStatus;
            }
            else if (person is Extern e)
            {
                Input.IsEntity = e.IsEntity;
                Input.Address = e.Address;
                Input.ExternStatus = e.ExternStatus;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var personToUpdate = await _context.People.FirstOrDefaultAsync(p => p.Id == Input.Id);
            if (personToUpdate == null) return NotFound();

            // Update common fields
            personToUpdate.Email = Input.Email?.Trim().ToLower();
            personToUpdate.PhoneNumber = Input.PhoneNumber?.Trim();
            personToUpdate.Status = Input.Status;

            if (personToUpdate is Intern i)
            {
                i.Name = Input.Name.Clean();
                if (Input.InternStatus.HasValue) i.InternStatus = Input.InternStatus.Value;
            }
            else if (personToUpdate is Extern e)
            {
                e.Name = Input.Name.Clean();
                e.IsEntity = Input.IsEntity;
                e.Address = Input.Address ?? "";
                if (Input.ExternStatus.HasValue) e.ExternStatus = Input.ExternStatus.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Datos de '{personToUpdate.FullName}' actualizados correctamente.");
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData.Error($"Error al actualizar: {ex.Message}");
                return Page();
            }
        }

        private bool PersonExists(int id)
        {
            return _context.People.Any(e => e.Id == id);
        }
    }
}
