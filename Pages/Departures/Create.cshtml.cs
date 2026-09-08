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
using System.ComponentModel.DataAnnotations;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Departures
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IManagementContextService managementService,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _managementService = managementService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "La facultad es obligatoria")]
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "El responsable/solicitante es obligatorio")]
            [Display(Name = "Responsable / Solicitante / Proveedor")]
            public int BorrowerId { get; set; }

            [Required(ErrorMessage = "El tipo de salida es obligatorio")]
            [Display(Name = "Tipo de Salida")]
            public DepartureType Type { get; set; } = DepartureType.ExternalMaintenance;

            [Required(ErrorMessage = "La fecha de salida es obligatoria")]
            [Display(Name = "Fecha de Salida")]
            [DataType(DataType.Date)]
            public DateTime DepartureDate { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "La fecha estimada de devolución es obligatoria")]
            [Display(Name = "Fecha Estimada de Devolución")]
            [DataType(DataType.Date)]
            public DateTime EstimatedReturnDate { get; set; } = DateTime.Today.AddDays(15);

            [StringLength(500)]
            [Display(Name = "Observaciones")]
            public string? DepartureObservations { get; set; }

            public List<ItemInput> Items { get; set; } = new();

            public class ItemInput
            {
                public int? Id { get; set; }
                public int? EquipmentUnitId { get; set; }
                public string ProductName { get; set; } = string.Empty;
                public int Quantity { get; set; } = 1;
                public string UnitOfMeasure { get; set; } = "UNIDAD";
                public string? Observations { get; set; }
            }
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false, int? managementPlanId = null)
        {
            ManagementPlanId = managementPlanId;
            ViewData["IsWizard"] = isWizard;

            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.Maintenance)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Equipment)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);

                ViewData["CurrentPhaseInt"] = (int)(plan?.CurrentPhase ?? WizardPhase.Exit);
                ViewData["ManagementId"] = plan?.ManagementId;

                if (plan?.ManagementId != null)
                {
                    var mgmt = await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == plan.ManagementId);
                    ViewData["IsCorrective"] = mgmt?.Type == ManagementType.Corrective;
                }

                // Exponer IDs de fases previas para la sección de referencia vinculada
                if (plan?.MaintenanceId != null)
                {
                    ViewData["LinkedMaintenanceId"] = plan.MaintenanceId;
                }

                if (isWizard)
                {
                    // En wizard: técnico SIEMPRE viene de L-8
                    if (plan?.Maintenance?.TechnicianId != null)
                    {
                        Input.BorrowerId = plan.Maintenance.TechnicianId.Value;
                        ViewData["IsLockedBorrower"] = true;

                        var technician = await _context.People.FindAsync(plan.Maintenance.TechnicianId.Value);
                        bool isExtern = technician is Extern || technician?.Category == PersonCategory.Externo;
                        ViewData["TechnicianName"] = technician is Intern i ? i.Name
                            : technician is Extern ext ? ext.Name
                            : "Técnico #" + plan.Maintenance.TechnicianId.Value;
                        ViewData["TechnicianIsExtern"] = isExtern;
                        ViewData["InferredTypeLabel"] = isExtern ? "Mantenimiento Externo" : "Mantenimiento Interno";
                        ViewData["TechnicianMissing"] = false;
                    }
                    else
                    {
                        // Sin técnico en L-8: bloquear hasta que lo asignen allá
                        ViewData["TechnicianMissing"] = true;
                    }
                }
                else if (plan?.Maintenance?.TechnicianId != null)
                {
                    // Fuera del wizard: precargar si hay vínculo
                    Input.BorrowerId = plan.Maintenance.TechnicianId.Value;
                    ViewData["IsLockedBorrower"] = true;
                }

                if (!equipmentUnitId.HasValue && plan?.EquipmentUnitId != null)
                {
                    equipmentUnitId = plan.EquipmentUnitId;
                }
            }

            if (equipmentUnitId.HasValue)
            {
                var unit = await _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Include(u => u.Laboratory)
                    .FirstOrDefaultAsync(u => u.Id == equipmentUnitId.Value);

                if (unit != null)
                {
                    Input.EquipmentUnitId = unit.Id;
                    Input.LaboratoryId = unit.LaboratoryId ?? 0;
                    Input.FacultyId = unit.Laboratory?.FacultyId ?? 0;

                    if (isWizard)
                    {
                        Input.Items.Add(new InputModel.ItemInput
                        {
                            EquipmentUnitId = unit.Id,
                            ProductName = $"{unit.Equipment?.Name ?? "Equipo"} ({unit.InventoryNumber ?? "—"})",
                            Quantity = 1,
                            UnitOfMeasure = "UNIDAD"
                        });
                    }
                }
            }

            await LoadLists();
            return Page();
        }

        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .OrderBy(x => x.name)
                .ToListAsync();
            return new JsonResult(labs);
        }

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

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            return await SaveDepartureAsync(isWizard, saveDraft: false);
        }

        public async Task<IActionResult> OnPostDraftAsync(bool isWizard = false)
        {
            return await SaveDepartureAsync(isWizard, saveDraft: true);
        }

        private async Task<IActionResult> SaveDepartureAsync(bool isWizard, bool saveDraft)
        {
            // ─────────────────────────────────────────────────────────────
            // WIZARD PRE-VALIDATION: reconstruir valores ANTES de ModelState
            // Los selects disabled no postean → FacultyId/LaboratoryId/
            // EquipmentUnitId/BorrowerId llegan como 0 → [Required] falla.
            // ─────────────────────────────────────────────────────────────
            ManagementPlan? wizardPlan = null;
            var managementId = (await _managementService.GetCurrentManagementAsync())?.Id ?? 0;

            if (ManagementPlanId.HasValue)
            {
                if (isWizard)
                {
                    wizardPlan = await _context.ManagementPlans
                        .AsTracking()
                        .Include(p => p.Maintenance)
                        .Include(p => p.Departure).ThenInclude(d => d!.Items)
                        .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Equipment)
                        .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Laboratory)
                        .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
                }
                else
                {
                    wizardPlan = await _context.ManagementPlans
                        .AsTracking()
                        .Include(p => p.Departure).ThenInclude(d => d!.Items)
                        .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
                }
                if (wizardPlan != null)
                    managementId = wizardPlan.ManagementId;
            }

            DepartureType inferredType = DepartureType.InternalLoan;
            Person? borrower = null;

            if (isWizard && wizardPlan != null)
            {
                // Técnico SIEMPRE viene de L-8 en wizard
                if (wizardPlan.Maintenance?.TechnicianId == null)
                {
                    TempData.Error("Debe asignar un técnico en L-8 antes de registrar la salida.");
                    await LoadLists();
                    return Page();
                }

                if (!wizardPlan.EquipmentUnitId.HasValue || wizardPlan.EquipmentUnit == null)
                {
                    TempData.Error("El plan de gestión no tiene una unidad de equipo vinculada.");
                    await LoadLists();
                    return Page();
                }

                // Reconstruir valores desde BD (disabled inputs NO postean)
                Input.BorrowerId = wizardPlan.Maintenance.TechnicianId.Value;
                Input.EquipmentUnitId = wizardPlan.EquipmentUnitId.Value;
                Input.LaboratoryId = wizardPlan.EquipmentUnit.LaboratoryId ?? 0;
                Input.FacultyId = wizardPlan.EquipmentUnit.Laboratory?.FacultyId ?? 0;

                borrower = await _context.People.FindAsync(Input.BorrowerId);
                inferredType = (borrower is Extern || borrower?.Category == PersonCategory.Externo)
                    ? DepartureType.ExternalMaintenance
                    : DepartureType.InternalMaintenance;

                // Limpiar keys que el servidor acaba de reconstruir
                ModelState.Remove("Input.FacultyId");
                ModelState.Remove("Input.LaboratoryId");
                ModelState.Remove("Input.EquipmentUnitId");
                ModelState.Remove("Input.BorrowerId");
                ModelState.Remove("Input.Type");
                // Items en wizard se generan server-side
                foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Input.Items")).ToList())
                    ModelState.Remove(key);
            }
            else if (isWizard)
            {
                TempData.Error($"No se pudo resolver el plan del wizard L-3. ManagementPlanId recibido: {ManagementPlanId?.ToString() ?? "sin valor"}.");
                await LoadLists();
                return Page();
            }

            if (saveDraft)
            {
                ModelState.Remove("Input.DepartureDate");
                ModelState.Remove("Input.EstimatedReturnDate");
            }

            // ── VALIDACIÓN después de reconstrucción ──
            if (!ModelState.IsValid)
            {
                var failedKeys = ModelState
                    .Where(ms => ms.Value != null && ms.Value.Errors.Count > 0)
                    .Select(ms => $"{ms.Key}: {string.Join(", ", ms.Value!.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();
                if (failedKeys.Any())
                    TempData.Error($"Campos con error: {string.Join(" | ", failedKeys)}");

                await LoadLists();
                return Page();
            }

            try
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                // Fuera del wizard: derivar tipo del técnico seleccionado en form
                if (!isWizard || borrower == null)
                {
                    borrower = await _context.People.FindAsync(Input.BorrowerId);
                    inferredType = (borrower is Extern || borrower?.Category == PersonCategory.Externo)
                        ? DepartureType.ExternalMaintenance
                        : DepartureType.InternalLoan;
                }

                Departure? departure;
                bool isNewDeparture = wizardPlan?.Departure == null;

                if (isNewDeparture)
                {
                    departure = new Departure
                    {
                        EquipmentUnitId = Input.EquipmentUnitId,
                        BorrowerId = Input.BorrowerId,
                        Type = inferredType,
                        DepartureDate = Input.DepartureDate,
                        EstimatedReturnDate = Input.EstimatedReturnDate,
                        DepartureObservations = Input.DepartureObservations?.Trim(),
                        Status = saveDraft ? LoanStatus.Cancelled : LoanStatus.Active,
                        CreatedDate = DateTime.UtcNow,
                        ManagementId = managementId
                    };

                    var currentUser = await _userManager.GetUserAsync(User);
                    departure.CreatedById = int.TryParse(currentUser?.Id.ToString(), out var uid) ? uid : (int?)null;

                    _context.Departures.Add(departure);
                }
                else
                {
                    departure = wizardPlan!.Departure!;
                    departure.DepartureDate = Input.DepartureDate;
                    departure.EstimatedReturnDate = Input.EstimatedReturnDate;
                    departure.DepartureObservations = Input.DepartureObservations?.Trim();
                    departure.Status = saveDraft ? LoanStatus.Cancelled : LoanStatus.Active;
                    departure.LastModifiedDate = DateTime.UtcNow;
                }

                // Ítem de salida — en wizard derivado desde BD, fuera del wizard desde form
                if (isWizard && wizardPlan?.EquipmentUnit != null)
                {
                    var unit = wizardPlan.EquipmentUnit;
                    var productName = $"{unit.Equipment?.Name ?? "Equipo"} ({unit.InventoryNumber ?? "S/N"})";

                    if (isNewDeparture)
                    {
                        _context.DepartureItems.Add(new DepartureItem
                        {
                            Departure = departure,
                            EquipmentUnitId = unit.Id,
                            ProductName = productName,
                            Quantity = 1,
                            UnitOfMeasure = "UNIDAD",
                            Observations = Input.Items.FirstOrDefault()?.Observations?.Trim()
                        });
                    }
                }
                else if (!isWizard && Input.Items != null && Input.Items.Count > 0)
                {
                    if (departure.Items != null && departure.Items.Any())
                    {
                        // Update existing items or add new ones
                        foreach (var itemInput in Input.Items.Where(i => !string.IsNullOrWhiteSpace(i.ProductName)))
                        {
                            var existing = departure.Items.FirstOrDefault(i => i.Id == itemInput.Id && itemInput.Id > 0);
                            if (existing != null)
                            {
                                existing.ProductName = itemInput.ProductName.Trim();
                                existing.Quantity = itemInput.Quantity;
                                existing.UnitOfMeasure = itemInput.UnitOfMeasure?.Trim() ?? "UNIDAD";
                                existing.Observations = itemInput.Observations?.Trim();
                            }
                            else
                            {
                                _context.DepartureItems.Add(new DepartureItem
                                {
                                    Departure = departure,
                                    EquipmentUnitId = itemInput.EquipmentUnitId ?? Input.EquipmentUnitId,
                                    ProductName = itemInput.ProductName.Trim(),
                                    Quantity = itemInput.Quantity,
                                    UnitOfMeasure = itemInput.UnitOfMeasure?.Trim() ?? "UNIDAD",
                                    Observations = itemInput.Observations?.Trim()
                                });
                            }
                        }
                    }
                    else
                    {
                        foreach (var itemInput in Input.Items.Where(i => !string.IsNullOrWhiteSpace(i.ProductName)))
                        {
                            _context.DepartureItems.Add(new DepartureItem
                            {
                                Departure = departure,
                                EquipmentUnitId = itemInput.EquipmentUnitId ?? Input.EquipmentUnitId,
                                ProductName = itemInput.ProductName.Trim(),
                                Quantity = itemInput.Quantity,
                                UnitOfMeasure = itemInput.UnitOfMeasure?.Trim() ?? "UNIDAD",
                                Observations = itemInput.Observations?.Trim()
                            });
                        }
                    }
                }

                if (!saveDraft)
                {
                    var equipmentUnit = await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);
                    if (equipmentUnit != null)
                    {
                        equipmentUnit.CurrentStatus = EquipmentStatus.OnLoan;
                        _context.EquipmentUnits.Update(equipmentUnit);
                    }
                }

                if (ManagementPlanId.HasValue)
                {
                    var plan = wizardPlan ?? await _context.ManagementPlans.FindAsync(ManagementPlanId.Value);
                    if (plan != null)
                    {
                        plan.Departure = departure;

                        if (saveDraft)
                        {
                            MarkDepartureDraft(plan);
                        }
                        else
                        {
                            plan.CurrentPhase = WizardPhase.Kardex;
                            plan.CurrentState = WizardEquipmentState.AwaitingKardex;
                            ClearDraft(plan);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                if (isWizard && wizardPlan != null)
                {
                    if (saveDraft)
                    {
                        TempData.Success("Borrador L-3 guardado correctamente.");
                        return RedirectToPage("/Index", new { ShowWizard = true, Step = 4, SelectedLabId = Input.LaboratoryId, ManagementId = wizardPlan.ManagementId });
                    }
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = 5, SelectedLabId = Input.LaboratoryId, ManagementId = wizardPlan.ManagementId });
                }

                TempData.Success(saveDraft ? "Borrador guardado correctamente." : "Salida de equipo registrada exitosamente.");
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al guardar L-3 para unidad {EquipmentUnitId} y plan {ManagementPlanId}.",
                    Input.EquipmentUnitId,
                    ManagementPlanId);
                TempData.Error("No se pudo guardar el registro L-3. Intente nuevamente.");
                await LoadLists();
                return Page();
            }
        }

        private static void MarkDepartureDraft(ManagementPlan plan)
        {
            plan.IsDraft = true;
            plan.DraftPhase = WizardPhase.Exit;
            plan.DraftSavedAt = DateTime.UtcNow;
            plan.DraftSummary = "Borrador L-3 guardado con salida parcial.";
            plan.CurrentPhase = WizardPhase.Exit;
            plan.CurrentState = WizardEquipmentState.AwaitingDeparture;
            plan.PlanStatus = ManagementPlanStatus.InProgress;
            plan.LastModifiedDate = DateTime.UtcNow;
        }

        private static void ClearDraft(ManagementPlan plan)
        {
            plan.IsDraft = false;
            plan.DraftPhase = null;
            plan.DraftSavedAt = null;
            plan.DraftSummary = null;
            plan.LastModifiedDate = DateTime.UtcNow;
        }

        private async Task LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name).ToListAsync(), "Id", "Name", Input.FacultyId);

            if (Input.FacultyId > 0)
                ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == Input.FacultyId).ToListAsync(), "Id", "Name", Input.LaboratoryId);
            else
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            if (Input.LaboratoryId > 0)
                ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == Input.LaboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", Input.EquipmentUnitId);
            else
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            // Fix: FullName is NotMapped and TPT inheritance causes translation issues.
            // We fetch the data and then evaluate the FullName in memory.
            var people = await _context.People
                .Where(p => p.Status == GeneralStatus.Activo)
                .ToListAsync();

            var borrowerList = people
                .Select(p => new { 
                    Id = p.Id, 
                    Name = p is Intern i ? i.Name : (p is Extern e ? e.Name : "Persona #" + p.Id) 
                })
                .OrderBy(x => x.Name)
                .ToList();

            ViewData["BorrowerId"] = new SelectList(borrowerList, "Id", "Name", Input.BorrowerId);
        }
    }
}
