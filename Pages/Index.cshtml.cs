using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Laboratorios_Univalle.Pages
{
    public class EquipmentSimulated
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InventoryNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int LabId { get; set; }
        public string StatusStep { get; set; } = string.Empty;
    }

    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Management? ActiveManagement { get; set; }
        public int TotalEquipment { get; set; }
        public int OperationalPercent { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowWizard { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public int Step { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; }

        public SelectList LaboratoriesList { get; set; } = default!;
        public List<EquipmentSimulated> EquipmentUnitsSimulated { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Buscamos si existe gestión en BD
                ActiveManagement = await _context.Managements
                    .Where(m => m.Status == ManagementStatus.Activo)
                    .OrderByDescending(m => m.Year)
                    .ThenByDescending(m => m.Semester)
                    .FirstOrDefaultAsync();
            }
            catch { }

            // Simulamos datos globales para que el dashboard nunca se vea vacío en la demo
            if (ActiveManagement == null)
            {
                ActiveManagement = new Management { Year = 2026, Semester = 1 };
            }
            TotalEquipment = 15;
            OperationalPercent = 85;

            if (ShowWizard)
            {
                LoadSimulatedData();
            }

            return Page();
        }

        public IActionResult OnPostNextStep()
        {
            return RedirectToPage(new { ShowWizard = true, Step = Step + 1, SelectedLabId = SelectedLabId });
        }

        public IActionResult OnPostPreviousStep()
        {
            int prevStep = Step > 1 ? Step - 1 : 1;
            return RedirectToPage(new { ShowWizard = true, Step = prevStep, SelectedLabId = SelectedLabId });
        }

        private void LoadSimulatedData()
        {
            var labs = new List<dynamic>
            {
                new { Id = 1, Name = "Civil" },
                new { Id = 2, Name = "H - 1" },
                new { Id = 3, Name = "Química" },
                new { Id = 4, Name = "Biotecnología" },
                new { Id = 5, Name = "Microbiología" },
                new { Id = 6, Name = "Electrónica" }
            };
            LaboratoriesList = new SelectList(labs, "Id", "Name", SelectedLabId);

            var allEquipment = new List<EquipmentSimulated>
            {
                new() { Id = 101, LabId = 1, Name = "Horno PINZUAR", InventoryNumber = "INV-11202", Category = "Equipos de Calentamiento", StatusStep = "Step1" },
                new() { Id = 102, LabId = 1, Name = "Prensa Ensayo Compresión", InventoryNumber = "INV-10900", Category = "Equipos de Ensayo", StatusStep = "Step2" }, // Simulado con falla para el Paso 2
                new() { Id = 103, LabId = 1, Name = "Tamizadora Eléctrica", InventoryNumber = "INV-10901", Category = "Equipos de Ensayo", StatusStep = "Step1" },

                new() { Id = 201, LabId = 2, Name = "Balanza Digital OHAUS", InventoryNumber = "INV-39170", Category = "Equipos de Medición", StatusStep = "Step1" },
                new() { Id = 202, LabId = 2, Name = "Campana Extracción WILDA", InventoryNumber = "INV-34745", Category = "Equipos de Ventilación", StatusStep = "Step2" }, // Simulado con falla para el Paso 2
                new() { Id = 203, LabId = 2, Name = "Microscopio Binocular", InventoryNumber = "INV-88211", Category = "Equipos de Observación", StatusStep = "Step2" },
                new() { Id = 204, LabId = 2, Name = "Centrífuga de Mesa", InventoryNumber = "INV-88300", Category = "Equipos de Proceso", StatusStep = "Step1" },

                new() { Id = 301, LabId = 3, Name = "Espectrofotómetro UV-VIS", InventoryNumber = "INV-22105", Category = "Equipos de Análisis", StatusStep = "Step1" },
                new() { Id = 302, LabId = 3, Name = "pH-metro Digital", InventoryNumber = "INV-22200", Category = "Equipos de Análisis", StatusStep = "Step1" },
                new() { Id = 303, LabId = 3, Name = "Agitador Magnético", InventoryNumber = "INV-22301", Category = "Equipos de Calentamiento", StatusStep = "Step1" }
            };

            if (SelectedLabId.HasValue)
            {
                EquipmentUnitsSimulated = allEquipment.Where(e => e.LabId == SelectedLabId.Value).ToList();
            }
        }
    }
}