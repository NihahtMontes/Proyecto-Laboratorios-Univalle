using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Proyecto_Laboratorios_Univalle.Pages
{
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
        public int? CurrentEquipmentUnitId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; } // Nueva propiedad para persistencia

        [BindProperty]
        public L6InputModel L6Input { get; set; } = new();

        public class L6InputModel
        {
            public int LaboratoryId { get; set; }
            public int EquipmentUnitId { get; set; }
            public VerificationResult GeneralCondition { get; set; }
            public string? Observations { get; set; }
        }

        [BindProperty]
        public L7InputModel L7Input { get; set; } = new();

        public class L7InputModel
        {
            public string Description { get; set; } = string.Empty;
            public RequestPriority Priority { get; set; } = RequestPriority.Medium;
            public string? EstimatedRepairTime { get; set; }
        }

        public SelectList LaboratoriesList { get; set; }
        public List<EquipmentSimulated> EquipmentUnitsSimulated { get; set; } = new();

        public class EquipmentSimulated
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string InventoryNumber { get; set; }
            public string Category { get; set; }
            public int LabId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                TotalEquipment = 15;
                ActiveManagement = new Management { Year = 2026, Semester = 1 };
            }
            catch (Exception) { ActiveManagement = null; }

            if (ShowWizard)
            {
                // Sincronizar L6Input con el ID de la URL
                if (SelectedLabId.HasValue) L6Input.LaboratoryId = SelectedLabId.Value;
                LoadSimulatedData();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostNextStepAsync()
        {
            if (Step == 1)
            {
                // Lógica de BD Real Comentada
                /* var user = await _userManager.GetUserAsync(User); ... */

                if (L6Input.GeneralCondition == VerificationResult.Good)
                    return RedirectToPage(new { ShowWizard = false });

                return RedirectToPage(new { ShowWizard = true, Step = 2, CurrentEquipmentUnitId = L6Input.EquipmentUnitId, SelectedLabId = L6Input.LaboratoryId });
            }
            return RedirectToPage(new { ShowWizard = true, Step = Step + 1, CurrentEquipmentUnitId = CurrentEquipmentUnitId, SelectedLabId = SelectedLabId });
        }

        public IActionResult OnPostPreviousStep()
        {
            int prevStep = Step > 1 ? Step - 1 : 1;
            return RedirectToPage(new { ShowWizard = true, Step = prevStep, CurrentEquipmentUnitId = CurrentEquipmentUnitId, SelectedLabId = SelectedLabId });
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
            LaboratoriesList = new SelectList(labs, "Id", "Name", L6Input.LaboratoryId);

            var allEquipment = new List<EquipmentSimulated>
            {
                new() { Id = 101, LabId = 1, Name = "Horno PINZUAR", InventoryNumber = "INV-11202", Category = "Equipos de Calentamiento" },
                new() { Id = 102, LabId = 1, Name = "Prensa Ensayo Compresión", InventoryNumber = "INV-10900", Category = "Equipos de Ensayo" },
                new() { Id = 103, LabId = 1, Name = "Tamizadora Eléctrica", InventoryNumber = "INV-10901", Category = "Equipos de Ensayo" },
                new() { Id = 104, LabId = 1, Name = "Permeámetro de Cabeza Constante", InventoryNumber = "INV-10902", Category = "Equipos de Ensayo" },
                new() { Id = 201, LabId = 2, Name = "Balanza Digital OHAUS", InventoryNumber = "INV-39170", Category = "Equipos de Medición" },
                new() { Id = 202, LabId = 2, Name = "Campana Extracción WILDA", InventoryNumber = "INV-34745", Category = "Equipos de Ventilación" },
                new() { Id = 203, LabId = 2, Name = "Microscopio Binocular", InventoryNumber = "INV-88211", Category = "Equipos de Observación" },
                new() { Id = 204, LabId = 2, Name = "Centrífuga de Mesa", InventoryNumber = "INV-88300", Category = "Equipos de Proceso" },
                new() { Id = 301, LabId = 3, Name = "Espectrofotómetro UV-VIS", InventoryNumber = "INV-22105", Category = "Equipos de Análisis" },
                new() { Id = 302, LabId = 3, Name = "pH-metro Digital", InventoryNumber = "INV-22200", Category = "Equipos de Análisis" },
                new() { Id = 303, LabId = 3, Name = "Agitador Magnético con Calefacción", InventoryNumber = "INV-22301", Category = "Equipos de Calentamiento" },
                new() { Id = 401, LabId = 4, Name = "FERMENTADOR ELECTRICO INOX", InventoryNumber = "INV-34363", Category = "Equipos de Proceso" },
                new() { Id = 501, LabId = 5, Name = "Autoclave vertical 50L", InventoryNumber = "INV-55901", Category = "Equipos de Esterilización" },
                new() { Id = 601, LabId = 6, Name = "Osciloscopio Digital RIGOL", InventoryNumber = "INV-77101", Category = "Equipos de Medición" },
                new() { Id = 602, LabId = 6, Name = "Fuente de Alimentación DC", InventoryNumber = "INV-77205", Category = "Equipos de Medición" },
                new() { Id = 603, LabId = 6, Name = "Multímetro de Banco FLUKE", InventoryNumber = "INV-77310", Category = "Equipos de Medición" }
            };

            if (L6Input.LaboratoryId > 0)
            {
                EquipmentUnitsSimulated = allEquipment.Where(e => e.LabId == L6Input.LaboratoryId).ToList();
            }
        }
    }
}