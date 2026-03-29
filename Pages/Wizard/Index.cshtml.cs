using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Proyecto_Laboratorios_Univalle.Pages.Wizard
{
    [Authorize]
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Step { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; }

        public SelectList LaboratoriesList { get; set; } = default!;
        public List<EquipmentSimulated> EquipmentUnitsSimulated { get; set; } = new();

        public class EquipmentSimulated
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string InventoryNumber { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public int LabId { get; set; }
            public string StatusStep { get; set; } = string.Empty; // Define en qué paso se muestra
        }

        public void OnGet()
        {
            if (Step < 1 || Step > 6) Step = 1;
            LoadSimulatedData();
        }

        public IActionResult OnPostNextStep()
        {
            return RedirectToPage(new { Step = Step + 1, SelectedLabId });
        }

        public IActionResult OnPostPreviousStep()
        {
            int prevStep = Step > 1 ? Step - 1 : 1;
            return RedirectToPage(new { Step = prevStep, SelectedLabId });
        }

        private void LoadSimulatedData()
        {
            var labs = new List<dynamic>
            {
                new { Id = 1, Name = "Civil" },
                new { Id = 2, Name = "H - 1" },
                new { Id = 3, Name = "Química" }
            };
            LaboratoriesList = new SelectList(labs, "Id", "Name", SelectedLabId);

            // Datos simulados preparados para la demostración
            var allEquipment = new List<EquipmentSimulated>
            {
                new() { Id = 101, LabId = 1, Name = "Horno PINZUAR", InventoryNumber = "INV-11202", Category = "Equipos de Calentamiento", StatusStep = "Step1" },
                new() { Id = 102, LabId = 1, Name = "Prensa Ensayo", InventoryNumber = "INV-10900", Category = "Equipos de Ensayo", StatusStep = "Step2" }, // Falla detectada
                new() { Id = 103, LabId = 1, Name = "Tamizadora Eléctrica", InventoryNumber = "INV-10901", Category = "Equipos de Ensayo", StatusStep = "Step3" }, // Requiere mantenimiento
                
                new() { Id = 201, LabId = 2, Name = "Balanza Digital OHAUS", InventoryNumber = "INV-39170", Category = "Equipos de Medición", StatusStep = "Step1" },
                new() { Id = 202, LabId = 2, Name = "Campana Extracción", InventoryNumber = "INV-34745", Category = "Equipos de Ventilación", StatusStep = "Step2" }
            };

            if (SelectedLabId.HasValue)
            {
                EquipmentUnitsSimulated = allEquipment.Where(e => e.LabId == SelectedLabId.Value).ToList();
            }
        }
    }
}