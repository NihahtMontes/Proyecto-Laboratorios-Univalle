using Microsoft.AspNetCore.Authorization;
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

namespace Proyecto_Laboratorios_Univalle.Pages.Schedules
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Management? ActiveManagement { get; set; }
        public IList<ManagementPlan> ManagementPlans { get; set; } = new List<ManagementPlan>();

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

        public async Task OnGetAsync()
        {
            // Cargar Listas para Filtros
            var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
            LabFList = new SelectList(labs, "Id", "Name");

            var techs = await _context.People
                .Where(p => p.Category == PersonCategory.Tecnico)
                .ToListAsync();
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
                var plansQuery = _context.ManagementPlans
                    .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                    .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                    .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                    .Include(p => p.Verification)
                    .Where(p => p.ManagementId == ActiveManagement.Id);

                // Aplicar filtros
                if (LabFilterId.HasValue) plansQuery = plansQuery.Where(p => p.EquipmentUnit!.LaboratoryId == LabFilterId);
                if (CategoryFilterId.HasValue) plansQuery = plansQuery.Where(p => p.EquipmentUnit!.Equipment!.Category == (EquipmentCategory)CategoryFilterId);
                if (TechFilterId.HasValue) plansQuery = plansQuery.Where(p => p.Maintenance != null && p.Maintenance.TechnicianId == TechFilterId);

                if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "Todos")
                {
                    if (StatusFilter == "Externo") 
                        plansQuery = plansQuery.Where(p => p.Maintenance != null && p.Maintenance.ServiceType == ServiceType.External);
                    else if (Enum.TryParse<ManagementPlanStatus>(StatusFilter, out var statusEnum))
                        plansQuery = plansQuery.Where(p => p.PlanStatus == statusEnum);
                }

                ManagementPlans = await plansQuery
                    .OrderBy(p => p.PlannedDate)
                    .ToListAsync(); // En esta página NO limitamos a 10, mostramos todo.
            }
        }
    }
}
