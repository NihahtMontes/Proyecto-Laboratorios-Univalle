using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    // =========================================================================
    // BASE DE DATOS TEMPORAL EN RAM (MOCK) PARA PROBAR EL DASHBOARD Y WIZARD
    // =========================================================================
    public static class MockDB
    {
        public static List<dynamic> Labs = new()
        {
            new { id = 1, name = "Civil" },
            new { id = 2, name = "H-1" },
            new { id = 3, name = "QuÃ­mica" },
            new { id = 4, name = "BiotecnologÃ­a" },
            new { id = 5, name = "MicrobiologÃ­a" },
            new { id = 6, name = "ElectrÃ³nica" }
        };

        public static List<dynamic> Units = new()
        {
            new { id = 101, labId = 1, name = "Horno PINZUAR", inventory = "INV-11202", category = "Equipos de Calentamiento" },
            new { id = 102, labId = 1, name = "Prensa Ensayo CompresiÃ³n", inventory = "INV-10900", category = "Equipos de Ensayo" },
            new { id = 103, labId = 1, name = "Tamizadora ElÃ©ctrica", inventory = "INV-10901", category = "Equipos de Ensayo" },
            new { id = 104, labId = 1, name = "PermeÃ¡metro de Cabeza Constante", inventory = "INV-10902", category = "Equipos de Ensayo" },

            new { id = 201, labId = 2, name = "Balanza Digital OHAUS", inventory = "INV-39170", category = "Equipos de MediciÃ³n" },
            new { id = 202, labId = 2, name = "Campana ExtracciÃ³n WILDA", inventory = "INV-34745", category = "Equipos de VentilaciÃ³n" },
            new { id = 203, labId = 2, name = "Microscopio Binocular", inventory = "INV-88211", category = "Equipos de ObservaciÃ³n" },
            new { id = 204, labId = 2, name = "CentrÃ­fuga de Mesa", inventory = "INV-88300", category = "Equipos de Proceso" },

            new { id = 301, labId = 3, name = "EspectrofotÃ³metro UV-VIS", inventory = "INV-22105", category = "Equipos de AnÃ¡lisis" },
            new { id = 302, labId = 3, name = "pH-metro Digital", inventory = "INV-22200", category = "Equipos de AnÃ¡lisis" },
            new { id = 303, labId = 3, name = "Agitador MagnÃ©tico con CalefacciÃ³n", inventory = "INV-22301", category = "Equipos de Calentamiento" },

            new { id = 401, labId = 4, name = "FERMENTADOR ELECTRICO INOX", inventory = "INV-34363", category = "Equipos de Proceso" },

            new { id = 501, labId = 5, name = "Autoclave vertical 50L", inventory = "INV-55901", category = "Equipos de EsterilizaciÃ³n" },

            new { id = 601, labId = 6, name = "Osciloscopio Digital RIGOL", inventory = "INV-77101", category = "Equipos de MediciÃ³n" },
            new { id = 602, labId = 6, name = "Fuente de AlimentaciÃ³n DC", inventory = "INV-77205", category = "Equipos de MediciÃ³n" },
            new { id = 603, labId = 6, name = "MultÃ­metro de Banco FLUKE", inventory = "INV-77310", category = "Equipos de MediciÃ³n" }
        };

        // --- NUEVA DATA PARA EL CRONOGRAMA DE INTERVENCIONES Y KARDEX ---
        public static List<dynamic> Interventions = new()
        {
            new { id=1, unitId=101, lab="Civil", eqName="Horno PINZUAR", inv="INV-11202", falla="Falla termostato", servicio="Externo", estado="Planeado", s12="", s13="Prev.", s14="", tech="PCS Proyectos", cat="Equipos de Calentamiento" },
            new { id=2, unitId=102, lab="Civil", eqName="Prensa Ensayo CompresiÃ³n", inv="INV-10900", falla="Fuga hidrÃ¡ulica", servicio="Interno", estado="Ejecutado", s12="OK", s13="", s14="", tech="Ing. Carlos Mamani", cat="Equipos de Ensayo" },
            new { id=3, unitId=103, lab="Civil", eqName="Tamizadora ElÃ©ctrica", inv="INV-10901", falla="Ninguna", servicio="Interno", estado="Ejecutado", s12="OK", s13="", s14="", tech="Ing. Carlos Mamani", cat="Equipos de Ensayo" },
            new { id=4, unitId=104, lab="Civil", eqName="PermeÃ¡metro de Cabeza Constante", inv="INV-10902", falla="Sello daÃ±ado", servicio="Interno", estado="En Progreso", s12="", s13="En curso", s14="", tech="Ing. Ana Flores", cat="Equipos de Ensayo" },
            new { id=5, unitId=201, lab="H-1", eqName="Balanza Digital OHAUS", inv="INV-39170", falla="Ninguna", servicio="Interno", estado="Ejecutado", s12="OK", s13="", s14="", tech="Ing. Sara PÃ©rez", cat="Equipos de MediciÃ³n" },
            new { id=6, unitId=202, lab="H-1", eqName="Campana ExtracciÃ³n WILDA", inv="INV-34745", falla="Ruido en motor", servicio="Externo", estado="Planeado", s12="", s13="Prev.", s14="", tech="TÃ©cnicos Industriales S.R.L.", cat="Equipos de VentilaciÃ³n" },
            new { id=7, unitId=203, lab="H-1", eqName="Microscopio Binocular", inv="INV-88211", falla="Ninguna", servicio="Interno", estado="Ejecutado", s12="OK", s13="", s14="", tech="Ing. Roberto Quispe", cat="Equipos de ObservaciÃ³n" },
            new { id=8, unitId=204, lab="H-1", eqName="CentrÃ­fuga de Mesa", inv="INV-88300", falla="VibraciÃ³n excesiva", servicio="Interno", estado="En Progreso", s12="", s13="En curso", s14="", tech="Ing. Sara PÃ©rez", cat="Equipos de Proceso" },
            new { id=9, unitId=301, lab="QuÃ­mica", eqName="EspectrofotÃ³metro UV-VIS", inv="INV-22105", falla="CalibraciÃ³n", servicio="Interno", estado="Planeado", s12="", s13="Prev.", s14="", tech="Ing. Ana Flores", cat="Equipos de AnÃ¡lisis" },
            new { id=10, unitId=302, lab="QuÃ­mica", eqName="pH-metro Digital", inv="INV-22200", falla="Electrodo deteriorado", servicio="Interno", estado="En Progreso", s12="", s13="En curso", s14="", tech="Ing. Ana Flores", cat="Equipos de AnÃ¡lisis" },
            new { id=11, unitId=303, lab="QuÃ­mica", eqName="Agitador MagnÃ©tico", inv="INV-22301", falla="Ninguna", servicio="Interno", estado="Ejecutado", s12="OK", s13="", s14="", tech="Ing. Roberto Quispe", cat="Equipos de Calentamiento" }
        };

        public static List<dynamic> HistoricKardex = new()
        {
            new { unitId=101, date="03 ENE 2024", action="InspecciÃ³n Preventiva", detail="RevisiÃ³n de termostato y resistencias calefactoras. Se detectÃ³ falla en termostato.", exec="Mantenimiento Interno (UMSA)", cost="0 Bs." },
            new { unitId=101, date="15 MAR 2023", action="Mantenimiento Correctivo", detail="Se reemplazÃ³ la resistencia principal del horno.", exec="PCS Proyectos", cost="1200 Bs." },
            new { unitId=101, date="10 NOV 2022", action="CalibraciÃ³n", detail="CalibraciÃ³n anual certificada.", exec="Ing. Carlos Mamani", cost="0 Bs." }
        };
        // ---------------------------------------------------------------------

        public static List<dynamic> L6_Verifications = new();
        public static List<dynamic> L7_Requests = new();
        public static List<dynamic> L8_Maintenances = new();
        public static List<int> DesembolsosSolicitados = new();

        public static void ResetRonda()
        {
            L6_Verifications.Clear();
            L7_Requests.Clear();
            L8_Maintenances.Clear();
            DesembolsosSolicitados.Clear();
        }
    }

    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- Active Management ---
        public Management? ActiveManagement { get; set; }
        
        // --- Core Stats ---
        public int TotalEquipment { get; set; }
        public int PendingRequests { get; set; }
        public int OngoingMaintenances { get; set; }
        public int RecentVerificationsCount { get; set; }
        public int OperationalPercent { get; set; } // Alison's metric
        
        // --- Active Management Metrics ---
        public Dictionary<string, int> LabProgress { get; set; } = new();
        public Dictionary<string, int> TypeProgress { get; set; } = new();
        public Dictionary<string, int> GroupProgress { get; set; } = new();
        public List<ManagementPlan> OverduePlans { get; set; } = new();
        public List<ManagementPlan> UpcomingPlans { get; set; } = new();
        public List<ManagementPlan> RecentActivity { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Core General Stats (Module 1/4/9)
            TotalEquipment = await _context.EquipmentUnits.CountAsync();
            PendingRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.Pending);
            OngoingMaintenances = await _context.Maintenances.CountAsync(m => m.Status == MaintenanceStatus.InProgress);
            
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            RecentVerificationsCount = await _context.Verifications.CountAsync(v => v.Date >= oneWeekAgo);

            if (TotalEquipment > 0)
            {
                int operational = await _context.EquipmentUnits.CountAsync(e => e.CurrentStatus == EquipmentStatus.Operational);
                OperationalPercent = (int)Math.Round((double)operational / TotalEquipment * 100);
            }
            else
            {
                OperationalPercent = 0;
            }

            // 2. Locate Active Management (Module 2 - L-48)
            ActiveManagement = await _context.Managements
                .Include(m => m.ManagementPlans)
                    .ThenInclude(p => p.EquipmentUnit)
                        .ThenInclude(eu => eu.Equipment)
                .Include(m => m.ManagementPlans)
                    .ThenInclude(p => p.EquipmentUnit)
                        .ThenInclude(eu => eu.Laboratory)
                .FirstOrDefaultAsync(m => m.Status == ManagementStatus.Activo);

            if (ActiveManagement != null)
            {
                // Calculate Metrics for Dashboard display
                LabProgress = ActiveManagement.ManagementPlans
                    .Where(p => p.EquipmentUnit?.Laboratory != null)
                    .GroupBy(p => p.EquipmentUnit.Laboratory.Name)
                    .ToDictionary(g => g.Key, g => (int)Math.Round((double)g.Count(p => p.PlanStatus == ManagementPlanStatus.Completed) / g.Count() * 100));

                TypeProgress = ActiveManagement.ManagementPlans
                    .Where(p => p.EquipmentUnit?.Equipment != null)
                    .GroupBy(p => p.EquipmentUnit.Equipment.Category.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                GroupProgress = ActiveManagement.ManagementPlans
                    .Where(p => p.EquipmentUnit?.Equipment != null)
                    .GroupBy(p => p.EquipmentUnit.Equipment.TypeClassification.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                OverduePlans = ActiveManagement.ManagementPlans
                    .Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value < DateTime.Now)
                    .OrderBy(p => p.PlannedDate)
                    .Take(3)
                    .ToList();

                UpcomingPlans = ActiveManagement.ManagementPlans
                    .Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value >= DateTime.Now)
                    .OrderBy(p => p.PlannedDate)
                    .Take(3)
                    .ToList();

                RecentActivity = ActiveManagement.ManagementPlans
                    .OrderByDescending(p => p.Id) 
                    .Take(5)
                    .ToList();
            }

            return Page();
        }

        // =========================================================================
        // AJAX ENDPOINTS PARA EL DASHBOARD PRINCIPAL (NUEVO)
        // =========================================================================
        public JsonResult OnGetDashboardInterventionsAsync()
        {
            return new JsonResult(MockDB.Interventions);
        }

        public JsonResult OnGetKardexHistoryAsync(int unitId)
        {
            var history = MockDB.HistoricKardex.Where(k => k.unitId == unitId).ToList();
            if (!history.Any())
            {
                history.Add(new { unitId = unitId, date = "01 ENE 2025", action = "Registro Inicial", detail = "Alta en el sistema.", exec = "Admin", cost = "0 Bs." });
            }
            return new JsonResult(history);
        }

        // =========================================================================
        // AJAX ENDPOINTS PARA EL WIZARD SPA 
        // =========================================================================
        public JsonResult OnGetWizardLabsAsync()
        {
            return new JsonResult(MockDB.Labs);
        }

        public JsonResult OnGetWizardUnitsL6Async(int labId)
        {
            var labUnits = MockDB.Units.Where(u => u.labId == labId).ToList();
            var result = labUnits.Select(u => {
                var ver = MockDB.L6_Verifications.FirstOrDefault(v => v.unitId == u.id);
                return new
                {
                    id = u.id,
                    name = u.name,
                    inventory = u.inventory,
                    category = u.category,
                    isVerified = ver != null,
                    isOk = ver != null && ver.status == 1,
                    faults = ver != null ? ver.faults : 0
                };
            });
            return new JsonResult(result);
        }

        [HttpPost]
        public JsonResult OnPostWizardVerifyL6Async(int unitId, int faultCount)
        {
            int status = faultCount == 0 ? 1 : 2;
            string obs = faultCount == 0 ? "Ninguna" : $"{faultCount} desperfecto(s) reportado(s)";
            MockDB.L6_Verifications.Add(new { unitId = unitId, status = status, faults = faultCount, obs = obs });
            return new JsonResult(new { success = true });
        }

        public JsonResult OnGetWizardFaultyUnitsL7Async(int labId)
        {
            var faultyUnitIds = MockDB.L6_Verifications.Where(v => v.status == 2).Select(v => v.unitId).ToList();
            var result = MockDB.Units.Where(u => u.labId == labId && faultyUnitIds.Contains(u.id)).Select(u => {
                var hasL7 = MockDB.L7_Requests.Any(r => r.unitId == u.id);
                var obs = MockDB.L6_Verifications.First(v => v.unitId == u.id).obs;
                return new
                {
                    id = u.id,
                    name = u.name,
                    inventory = u.inventory,
                    category = u.category,
                    faultDetails = obs,
                    hasRequest = hasL7
                };
            });
            return new JsonResult(result);
        }

        [HttpPost]
        public JsonResult OnPostWizardGenerateL7Async(int unitId, int labId)
        {
            MockDB.L7_Requests.Add(new { reqId = MockDB.L7_Requests.Count + 1000, unitId = unitId, labId = labId });
            return new JsonResult(new { success = true });
        }

        public JsonResult OnGetWizardPendingL8Async(int labId)
        {
            var pendingRequests = MockDB.L7_Requests.Where(r => r.labId == labId).ToList();
            var result = pendingRequests.Select(r => {
                var unit = MockDB.Units.First(u => u.id == r.unitId);
                var hasL8 = MockDB.L8_Maintenances.Any(m => m.reqId == r.reqId);
                return new
                {
                    id = unit.id,
                    requestId = r.reqId,
                    name = unit.name,
                    inventory = unit.inventory,
                    category = unit.category,
                    description = "Mantenimiento Correctivo asignado.",
                    isMaintained = hasL8
                };
            });
            return new JsonResult(result);
        }

        [HttpPost]
        public JsonResult OnPostWizardSaveL8Async(int unitId, int requestId, int labId, decimal cost, string description)
        {
            MockDB.L8_Maintenances.Add(new
            {
                unitId = unitId,
                reqId = requestId,
                labId = labId,
                cost = cost,
                desc = string.IsNullOrEmpty(description) ? "Limpieza y reemplazo de componentes" : description,
                tech = "TÃ©cnico Univalle",
                date = DateTime.UtcNow.ToString("dd/MM/yyyy")
            });
            return new JsonResult(new { success = true });
        }

        public JsonResult OnGetWizardCompletedL8ForL3Async(int labId)
        {
            var result = MockDB.L8_Maintenances.Where(m => m.labId == labId).Select(m => {
                var unit = MockDB.Units.First(u => u.id == m.unitId);
                return new
                {
                    id = unit.id,
                    maintenanceId = m.reqId + 500,
                    name = unit.name,
                    inventory = unit.inventory,
                    category = unit.category,
                    cost = m.cost,
                    description = m.desc
                };
            });
            return new JsonResult(result);
        }

        public JsonResult OnGetWizardKardexAsync(int labId)
        {
            var result = MockDB.L8_Maintenances.Where(m => m.labId == labId).Select(m => {
                var unit = MockDB.Units.First(u => u.id == m.unitId);
                return new
                {
                    id = unit.id,
                    name = unit.name,
                    inventory = unit.inventory,
                    category = unit.category,
                    technician = m.tech,
                    date = m.date,
                    cost = m.cost
                };
            });
            return new JsonResult(result);
        }

        public JsonResult OnGetWizardDesembolsoAsync(int labId)
        {
            var maintenances = MockDB.L8_Maintenances.Where(m => m.labId == labId).ToList();
            decimal totalCost = maintenances.Sum(m => (decimal)m.cost);
            var details = maintenances.Select(m => {
                var unit = MockDB.Units.First(u => u.id == m.unitId);
                return new { id = unit.id, name = unit.name, inventory = unit.inventory, cost = m.cost, isRequested = MockDB.DesembolsosSolicitados.Contains((int)unit.id) };
            });
            return new JsonResult(new { total = totalCost, items = details });
        }

        [HttpPost]
        public JsonResult OnPostWizardRequestDesembolsoAsync(int unitId)
        {
            if (!MockDB.DesembolsosSolicitados.Contains(unitId)) MockDB.DesembolsosSolicitados.Add(unitId);
            return new JsonResult(new { success = true });
        }

        [HttpPost]
        public async Task<JsonResult> OnPostWizardFinishRoundAsync(int labId)
        {
            await Task.Delay(800);
            MockDB.ResetRonda();
            return new JsonResult(new { success = true });
        }
    }
}
