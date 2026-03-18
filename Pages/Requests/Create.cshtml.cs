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
            await LoadLists();
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public RequestType Type { get; set; } = RequestType.Technical;

            [Required(ErrorMessage = "La facultad es obligatoria")]
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física (Activo)")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "La descripción del fallo es obligatoria")]
            [Display(Name = "Descripción del Fallo")]
            [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres")]
            public string Description { get; set; } = string.Empty;

            [Display(Name = "Observaciones Adicionales")]
            [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres")]
            public string? Observations { get; set; }

            [Display(Name = "Prioridad")]
            public RequestPriority Priority { get; set; } = RequestPriority.Medium;

            [Display(Name = "Tiempo Estimado de Reparación")]
            [StringLength(100)]
            public string? EstimatedRepairTime { get; set; }
        }

        // AJAX Handler: Get laboratories by faculty
        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .OrderBy(x => x.name)
                .ToListAsync();
            return new JsonResult(labs);
        }

        // AJAX Handler: Get units by laboratory
        public async Task<JsonResult> OnGetUnitsByLabAsync(int laboratoryId)
        {
            var units = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(u => u.Equipment!.Name)
                .ThenBy(u => u.InventoryNumber)
                .Select(u => new {
                    id = u.Id,
                    eqName = u.Equipment != null ? u.Equipment.Name : "Equipo",
                    inv = u.InventoryNumber
                })
                .ToListAsync();

            var result = units.Select(x => new {
                id = x.id,
                name = $"{x.eqName} (Inv: {x.inv})"
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Forzamos el tipo a Técnico por seguridad
            Input.Type = RequestType.Technical;

            if (!ModelState.IsValid)
            {
                await LoadLists();
                return Page();
            }

            var unit = await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);
            if (unit == null)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "La unidad seleccionada no es válida.");
                await LoadLists();
                return Page();
            }

            var request = new Request
            {
                Type = Input.Type,
                LaboratoryId = Input.LaboratoryId,
                EquipmentId = unit.EquipmentId,
                EquipmentUnitId = unit.Id,
                Description = Input.Description.Clean()!,
                Priority = Input.Priority,
                Observations = Input.Observations?.Clean(),
                EstimatedRepairTime = Input.EstimatedRepairTime?.Clean(),
                Status = RequestStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                request.CreatedById = currentUser.Id;
                request.RequestedById = currentUser.Id;
            }

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            TempData.Success($"Solicitud técnica L-7 para '{unit.InventoryNumber}' registrada exitosamente.");
            return RedirectToPage("./Index");
        }

        private async Task LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name)
                .ToListAsync(), "Id", "Name");

            ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}