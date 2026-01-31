using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await CargarListas();
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El equipamiento es obligatorio")]
            [Display(Name = "Equipamiento")]
            public int EquipmentId { get; set; }

            [Display(Name = "Solicitado Por")]
            public int? RequestedById { get; set; }

            [Required(ErrorMessage = "La descripción del problema es obligatoria")]
            [Display(Name = "Descripción del Problema")]
            [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres")]
            public string Description { get; set; } = string.Empty;

            [Required(ErrorMessage = "La prioridad es obligatoria")]
            [Display(Name = "Prioridad")]
            public RequestPriority Priority { get; set; } = RequestPriority.Medium;

            [Display(Name = "Observaciones Adicionales")]
            [StringLength(500)]
            public string? Observations { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await CargarListas();
                return Page();
            }

            var request = new Request
            {
                EquipmentId = Input.EquipmentId,
                Description = Input.Description.Clean(),
                Priority = Input.Priority,
                Observations = Input.Observations?.Clean(),
                Status = RequestStatus.Pending,
                CreatedDate = DateTime.Now
            };

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                request.CreatedById = currentUser.Id;
                // Si no se seleccionó un solicitante específico, asumimos el usuario actual
                request.RequestedById = Input.RequestedById ?? currentUser.Id;
            }

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            // Obtener nombre del equipo para el mensaje
            var equipmentName = await _context.Equipments
                .Where(e => e.Id == request.EquipmentId)
                .Select(e => e.Name)
                .FirstOrDefaultAsync();

            TempData.Success(NotificationHelper.Requests.Created(equipmentName ?? "Equipo"));
            return RedirectToPage("./Index");
        }

        private async Task CargarListas()
        {
            var equipos = await _context.Equipments
                .OrderBy(e => e.Name)
                .Select(e => new { 
                    Id = e.Id, 
                    Name = $"{e.Name} (Inv: {e.InventoryNumber})" 
                }).ToListAsync();

            ViewData["EquipmentId"] = new SelectList(equipos, "Id", "Name");

            var usuarios = await _context.Users
                .Where(u => u.Status == GeneralStatus.Activo)
                .OrderBy(u => u.FirstName)
                .Select(u => new { 
                    Id = u.Id, 
                    FullName = u.FullName 
                }).ToListAsync();

            ViewData["RequestedById"] = new SelectList(usuarios, "Id", "FullName");
        }
    }
}
