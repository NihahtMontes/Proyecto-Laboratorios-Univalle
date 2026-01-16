using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
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
            ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
            // Assuming RequestedBy is usually the current user or selectable from list. Original used all users.
            ViewData["RequestedById"] = new SelectList(_context.Users, "Id", "FullName");
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public int EquipmentId { get; set; }
            public string? RequestedById { get; set; }
            [Required]
            public string Description { get; set; }
            public RequestPriority Priority { get; set; }
            public string? Observations { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
                ViewData["RequestedById"] = new SelectList(_context.Users, "Id", "FullName");
                return Page();
            }

            var request = new Request
            {
                EquipmentId = Input.EquipmentId,
                Description = Input.Description,
                Priority = Input.Priority,
                Observations = Input.Observations,
                CreatedDate = DateTime.Now
            };

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                if (string.IsNullOrEmpty(Input.RequestedById))
                {
                    request.RequestedById = currentUser.Id;
                }
                else
                {
                    request.RequestedById = int.TryParse(Input.RequestedById, out var requestedByIdValue) ? requestedByIdValue : (int?)null;
                }
            }

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
