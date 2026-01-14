using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Laboratorios_Univalle.Pages.Laboratories
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

        [BindProperty]
        public LabInputModel Input { get; set; } = new();

        public int Id { get; set; } // To keep track of ID in the URL/Form

        public class LabInputModel
        {
            public int FacultyId { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Type { get; set; }
            public string? Building { get; set; }
            public string? Floor { get; set; }
            public string? Description { get; set; }
            public GeneralStatus Status { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var laboratory = await _context.Laboratories.FirstOrDefaultAsync(m => m.Id == id);
            if (laboratory == null) return NotFound();

            Id = laboratory.Id;

            // Map Entity -> DTO
            Input = new LabInputModel
            {
                FacultyId = laboratory.FacultyId,
                Code = laboratory.Code,
                Name = laboratory.Name,
                Type = laboratory.Type,
                Building = laboratory.Building,
                Floor = laboratory.Floor,
                Description = laboratory.Description,
                Status = laboratory.Status
            };

            CargarEstados();
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name", laboratory.FacultyId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Id = id; // Ensure ID is captured from route
            if (!ModelState.IsValid)
            {
                CargarEstados();
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name");
                return Page();
            }

            // 1. Uniqueness Check (Ignoring Deleted)
            // "Is there any OTHER lab (not me) with the same Code that is NOT Deleted?"
            bool codeExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Code == Input.Code && 
                               l.Id != id && 
                               l.Status != GeneralStatus.Eliminado);

            if (codeExists)
            {
                ModelState.AddModelError("Input.Code", "El código (Siglas) ya está en uso por otro laboratorio activo.");
            }

            // Check Name uniqueness as well
            bool nameExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Name == Input.Name && 
                               l.Id != id && 
                               l.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "El nombre del laboratorio ya está en uso por otro laboratorio activo.");
            }

            if (!ModelState.IsValid)
            {
                CargarEstados();
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name");
                return Page();
            }

            // 2. Fetch Original Entity
            var laboratory = await _context.Laboratories.FindAsync(id);
            if (laboratory == null) return NotFound();

            // 3. Update Fields (DTO -> Entity)
            laboratory.FacultyId = Input.FacultyId;
            laboratory.Code = Input.Code;
            laboratory.Name = Input.Name;
            laboratory.Type = Input.Type;
            laboratory.Building = Input.Building;
            laboratory.Floor = Input.Floor;
            laboratory.Description = Input.Description;
            laboratory.Status = Input.Status;

            // 4. Audit Protection (Redundant but safe)
            // CreatedBy/Date are NOT touched here. ModifiedBy/Date are handled by DbContext.SaveAsync override.

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LaboratoryExists(laboratory.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private bool LaboratoryExists(int id)
        {
            return _context.Laboratories.Any(e => e.Id == id);
        }

        private void CargarEstados()
        {
            var estados = EnumHelper.GetStatusSelectList<GeneralStatus>();
            ViewData["ListaEstados"] = new SelectList(estados, "Value", "Text", (int)Input.Status);
        }
    }
}
