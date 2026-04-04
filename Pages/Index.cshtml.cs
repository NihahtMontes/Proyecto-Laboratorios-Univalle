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
        
        // Propiedades Simuladas Antiguas (Se conservan para cuando NO haya Gestión Activa)
        public int TotalEquipment { get; set; }
        public int OperationalPercent { get; set; }

        // Nuevas Propiedades Reales para el Dashboard 
        public int TotalPlans { get; set; }
        public int CompletedPlans { get; set; }
        public double GlobalProgress { get; set; }
        public Dictionary<string, int> TopEquipmentTypes { get; set; } = new();
        public Dictionary<string, int> TopGroups { get; set; } = new();
        public Dictionary<string, int> TopLaboratories { get; set; } = new();
        public IList<ManagementPlan> OverduePlans { get; set; } = new List<ManagementPlan>();
        public IList<ManagementPlan> ManagementPlans { get; set; } = new List<ManagementPlan>();

        // Propiedades de Filtrado para el Cronograma (L-48 / Dashboard)
        [BindProperty(SupportsGet = true)]
        public int? LabFilterId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilterId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? TechFilterId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public SelectList LabFList { get; set; } = default!;
        public SelectList CategoryFList { get; set; } = default!;
        public SelectList TechFList { get; set; } = default!;

        // Propiedades del Wizard Embebido
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
                // Cargar Listas para Filtros (Usar Id para ordenar ya que FullName no está mapeado)
                var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
                LabFList = new SelectList(labs, "Id", "Name");

                var techs = await _context.People
                    .Where(p => p.Category == PersonCategory.Tecnico)
                    .ToListAsync(); // Traer a memoria para poder usar FullName (que no está mapeado)
                
                TechFList = new SelectList(techs.OrderBy(t => t.FullName), "Id", "FullName");

                CategoryFList = new SelectList(Enum.GetValues(typeof(EquipmentCategory))
                    .Cast<EquipmentCategory>()
                    .Select(e => new { Id = (int)e, Name = e.ToString() }), "Id", "Name");

                // Buscar Gestión Activa
                ActiveManagement = await _context.Managements
                    .Where(m => m.Status == ManagementStatus.Active)
                    .OrderByDescending(m => m.Year)
                    .ThenByDescending(m => m.Semester)
                    .FirstOrDefaultAsync();

                if (ActiveManagement != null)
                {
                    // Query Base para el Dashboard y Cronograma
                    var plansQuery = _context.ManagementPlans
                        .Include(p => p.EquipmentUnit).ThenInclude(eu => eu.Equipment)
                        .Include(p => p.EquipmentUnit).ThenInclude(eu => eu.Laboratory)
                        .Include(p => p.Maintenance).ThenInclude(m => m.Technician)
                        .Include(p => p.Verification)
                        .Where(p => p.ManagementId == ActiveManagement.Id);

                    // Estadísticas Globales (Sin Filtros del Cronograma)
                    var allPlans = await plansQuery.ToListAsync();
                    TotalPlans = allPlans.Count;
                    CompletedPlans = allPlans.Count(p => p.PlanStatus == ManagementPlanStatus.Completed);
                    GlobalProgress = TotalPlans > 0 ? Math.Round((double)CompletedPlans / TotalPlans * 100, 1) : 0;

                    TopEquipmentTypes = allPlans
                        .Where(p => p.EquipmentUnit?.Equipment != null)
                        .GroupBy(p => p.EquipmentUnit.Equipment.Category.ToString())
                        .OrderByDescending(g => g.Count())
                        .ToDictionary(g => g.Key, g => g.Count());

                    TopGroups = allPlans
                        .Where(p => p.EquipmentUnit?.Equipment != null)
                        .GroupBy(p => p.EquipmentUnit.Equipment.TypeClassification.ToString())
                        .OrderByDescending(g => g.Count())
                        .ToDictionary(g => g.Key, g => g.Count());

                    TopLaboratories = allPlans
                        .Where(p => p.EquipmentUnit?.Laboratory != null)
                        .GroupBy(p => p.EquipmentUnit.Laboratory.Name)
                        .OrderByDescending(g => g.Count())
                        .ToDictionary(g => g.Key, g => g.Count());

                    OverduePlans = allPlans
                        .Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value < DateTime.Now.AddDays(7))
                        .OrderBy(p => p.PlannedDate)
                        .ToList();

                    // APLICAR FILTROS AL CRONOGRAMA
                    var cronogramaQuery = plansQuery.AsQueryable();

                    if (LabFilterId.HasValue) cronogramaQuery = cronogramaQuery.Where(p => p.EquipmentUnit.LaboratoryId == LabFilterId);
                    if (CategoryFilterId.HasValue) cronogramaQuery = cronogramaQuery.Where(p => p.EquipmentUnit.Equipment.Category == (EquipmentCategory)CategoryFilterId);
                    if (TechFilterId.HasValue) cronogramaQuery = cronogramaQuery.Where(p => p.Maintenance.TechnicianId == TechFilterId);
                    
                    if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "Todos")
                    {
                        if (StatusFilter == "Externo") 
                            cronogramaQuery = cronogramaQuery.Where(p => p.Maintenance.ServiceType == ServiceType.External);
                        else if (Enum.TryParse<ManagementPlanStatus>(StatusFilter, out var statusEnum))
                            cronogramaQuery = cronogramaQuery.Where(p => p.PlanStatus == statusEnum);
                    }

                    ManagementPlans = await cronogramaQuery
                        .OrderBy(p => p.PlannedDate)
                        .Take(10)
                        .ToListAsync();
                }
            }
            catch (Exception ex) 
            {
                // Log error if needed
            }

            // Simulamos datos globales para que si no hay ActiveManagement, el dashboard nunca se vea vacío
            if (ActiveManagement == null)
            {
                TotalEquipment = 15;
                OperationalPercent = 85;
            }

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
                new() { Id = 102, LabId = 1, Name = "Prensa Ensayo Compresión", InventoryNumber = "INV-10900", Category = "Equipos de Ensayo", StatusStep = "Step2" }, 
                new() { Id = 103, LabId = 1, Name = "Tamizadora Eléctrica", InventoryNumber = "INV-10901", Category = "Equipos de Ensayo", StatusStep = "Step1" },

                new() { Id = 201, LabId = 2, Name = "Balanza Digital OHAUS", InventoryNumber = "INV-39170", Category = "Equipos de Medición", StatusStep = "Step1" },
                new() { Id = 202, LabId = 2, Name = "Campana Extracción WILDA", InventoryNumber = "INV-34745", Category = "Equipos de Ventilación", StatusStep = "Step2" }, 
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