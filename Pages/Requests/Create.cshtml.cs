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

        // Agregado isWizard y equipmentUnitId para pre-carga
        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false)
        {
            await LoadLists();

            if (equipmentUnitId.HasValue)
            {
                var unit = await _context.EquipmentUnits.FindAsync(equipmentUnitId.Value);
                if (unit != null)
                {
                    Input.FacultyId = unit.Laboratory?.FacultyId ?? 0;
                    Input.LaboratoryId = unit.LaboratoryId;
                    Input.EquipmentUnitId = unit.Id;
                }
            }

            ViewData["IsWizard"] = isWizard;
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public RequestType Type { get; set; } = RequestType.Technical;
            [Required(ErrorMessage = "La facultad es obligatoria")]
            public int FacultyId { get; set; }
            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            public int LaboratoryId { get; set; }
            [Required(ErrorMessage = "La unidad física es obligatoria")]
            public int EquipmentUnitId { get; set; }
            [Required(ErrorMessage = "La descripción del fallo es obligatoria")]
            public string Description { get; set; } = string.Empty;
            public string? Observations { get; set; }
            public RequestPriority Priority { get; set; } = RequestPriority.Medium;
            public string? EstimatedRepairTime { get; set; }
        }

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            Input.Type = RequestType.Technical;

            if (!ModelState.IsValid)
            {
                await LoadLists();
                return Page();
            }

            var unit = await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);

            // LOGICA DE BASE DE DATOS REAL (CONEXION)
            var request = new Request
            {
                Type = Input.Type,
                LaboratoryId = Input.LaboratoryId,
                EquipmentId = unit?.EquipmentId ?? 0,
                EquipmentUnitId = Input.EquipmentUnitId,
                Description = Input.Description.Clean()!,
                Priority = Input.Priority,
                Observations = Input.Observations?.Clean(),
                EstimatedRepairTime = Input.EstimatedRepairTime?.Clean(),
                Status = RequestStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null) { request.CreatedById = currentUser.Id; request.RequestedById = currentUser.Id; }

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            TempData.Success($"Solicitud técnica L-7 registrada exitosamente.");

            // REDIRECCIÓN INTELIGENTE
            if (isWizard)
            {
                // Vuelve al Wizard al paso 2 (donde estaba) o al 3
                return RedirectToPage("/Index", new { ShowWizard = true, Step = 2 });
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties.Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name).ToListAsync(), "Id", "Name");
            ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}