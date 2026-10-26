using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class MassCreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementContext;
        private readonly ILogger<MassCreateModel> _logger;

        public MassCreateModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            UserManager<User> userManager,
            IManagementContextService managementContext,
            ILogger<MassCreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _managementContext = managementContext;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public bool IsWizard { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? LabId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        [BindProperty]
        public List<RowInput> Rows { get; set; } = new();

        public string? LabName { get; set; }
        public SelectList LabList { get; set; } = default!;

        public class RowInput
        {
            public int PlanId { get; set; }
            public int EquipmentUnitId { get; set; }
            public PhysicalCondition Condition { get; set; }
            public string? Observations { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (!ManagementId.HasValue)
            {
                ManagementId = (await _managementContext.GetCurrentManagementAsync())?.Id;
            }

            var labs = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Name)
                .ToListAsync();
            LabList = new SelectList(labs, "Id", "Name", LabId);

            if (LabId.HasValue)
            {
                var lab = await _context.Laboratories.FindAsync(LabId.Value);
                LabName = lab?.Name;
            }

            ViewData["IsWizard"] = IsWizard;
            ViewData["ManagementId"] = ManagementId;
        }

        public async Task<JsonResult> OnGetEquipmentByLab(int labId, int? managementId = null)
        {
            var activeMgmt = managementId.HasValue
                ? await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == managementId.Value)
                : await _managementContext.GetCurrentManagementAsync();
            if (activeMgmt == null)
                return new JsonResult(Array.Empty<object>());

            var plans = await _context.ManagementPlans
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Include(p => p.Verification)
                .Where(p => p.ManagementId == activeMgmt.Id
                          && p.EquipmentUnit!.LaboratoryId == labId
                          && (p.VerificationId == null || p.Verification!.Status == VerificationStatus.Draft)
                          && p.CurrentPhase == WizardPhase.Verification)
                .AsNoTracking()
                .OrderBy(p => p.EquipmentUnit!.InventoryNumber)
                .ToListAsync();

            var result = plans.Select(p => new
            {
                planId = p.Id,
                equipmentUnitId = p.EquipmentUnitId,
                name = p.EquipmentUnit?.Equipment?.Name ?? "Sin nombre",
                inventoryNumber = p.EquipmentUnit?.InventoryNumber ?? "N/A",
                brand = p.EquipmentUnit?.Equipment?.Brand ?? "N/A",
                currentStatus = p.EquipmentUnit?.CurrentStatus.ToString() ?? "N/A",
                condition = (int)(p.Verification?.PhysicalCondition ?? PhysicalCondition.Good),
                observations = p.Verification?.Observations ?? string.Empty,
                isDraft = p.Verification?.Status == VerificationStatus.Draft
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return await SaveVerificationsAsync(saveDraft: false);
        }

        public async Task<IActionResult> OnPostDraftAsync()
        {
            return await SaveVerificationsAsync(saveDraft: true);
        }

        private async Task<IActionResult> SaveVerificationsAsync(bool saveDraft)
        {
            if (Rows == null || Rows.Count == 0)
            {
                TempData.Error("No hay equipos para procesar.");
                return RedirectToPage(new { isWizard = IsWizard, labId = LabId, managementId = ManagementId });
            }

            var activeMgmt = ManagementId.HasValue
                ? await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == ManagementId.Value)
                : await _managementContext.GetCurrentManagementAsync();
            if (activeMgmt == null || activeMgmt.Status != ManagementStatus.Active || activeMgmt.Type != ManagementType.Preventive)
            {
                TempData.Error("Seleccione una gestión preventiva activa para registrar la verificación.");
                return RedirectToPage();
            }

            if (!LabId.HasValue || Rows.GroupBy(r => r.PlanId).Any(g => g.Key <= 0 || g.Count() > 1))
            {
                TempData.Error("Seleccione un laboratorio y envíe cada equipo una sola vez.");
                return RedirectToPage(new { isWizard = IsWizard, labId = LabId, managementId = activeMgmt.Id });
            }

            // Validar todas las filas antes de escribir: los IDs del formulario no son autoridad.
            foreach (var batch in Rows.Chunk(100))
            {
                var ids = batch.Select(r => r.PlanId).ToArray();
                var validPlans = await _context.ManagementPlans.AsNoTracking()
                    .Where(p => ids.Contains(p.Id) && p.ManagementId == activeMgmt.Id
                        && p.CurrentPhase == WizardPhase.Verification && p.EquipmentUnit != null
                        && p.EquipmentUnit.LaboratoryId == LabId.Value)
                    .Select(p => new { p.Id, p.EquipmentUnitId, p.VerificationId })
                    .ToDictionaryAsync(p => p.Id);

                foreach (var row in batch)
                {
                    if (!validPlans.TryGetValue(row.PlanId, out var plan)
                        || plan.EquipmentUnitId != row.EquipmentUnitId
                        || !Enum.IsDefined(row.Condition))
                    {
                        TempData.Error("Los equipos o su paso en el asistente cambiaron. Recargue el formulario antes de guardar.");
                        return RedirectToPage(new { isWizard = IsWizard, labId = LabId, managementId = activeMgmt.Id });
                    }

                    if (row.Condition == PhysicalCondition.Bad && string.IsNullOrWhiteSpace(row.Observations))
                    {
                        TempData.Error("Describa las fallas de cada equipo marcado en estado Malo antes de guardar la verificación.");
                        return RedirectToPage(new { isWizard = IsWizard, labId = LabId, managementId = activeMgmt.Id });
                    }

                    if (plan.VerificationId.HasValue && !await _context.Verifications.AsNoTracking()
                        .AnyAsync(v => v.Id == plan.VerificationId.Value && v.ManagementId == activeMgmt.Id
                            && v.EquipmentUnitId == row.EquipmentUnitId && v.Status == VerificationStatus.Draft))
                    {
                        TempData.Error("Una verificación ya fue finalizada o no pertenece a este equipo. Recargue el formulario.");
                        return RedirectToPage(new { isWizard = IsWizard, labId = LabId, managementId = activeMgmt.Id });
                    }
                }
            }

            var user = await _userManager.GetUserAsync(User);
            int created = 0;
            int badCount = 0;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var batch in Rows.Chunk(100))
                {
                    foreach (var row in batch)
                    {
                        var plan = await _context.ManagementPlans
                            .AsTracking()
                            .FirstOrDefaultAsync(p => p.Id == row.PlanId && p.ManagementId == activeMgmt.Id
                                && p.EquipmentUnitId == row.EquipmentUnitId && p.CurrentPhase == WizardPhase.Verification);

                        if (plan == null) throw new InvalidOperationException("Un equipo cambió mientras se guardaba.");

                        var verification = plan.VerificationId.HasValue
                            ? await _context.Verifications.AsTracking().FirstOrDefaultAsync(v => v.Id == plan.VerificationId.Value)
                            : null;

                        if (plan.VerificationId.HasValue && verification?.Status != VerificationStatus.Draft)
                            throw new InvalidOperationException("La verificación cambió mientras se guardaba.");

                        if (verification == null)
                        {
                            verification = new Verification
                            {
                                EquipmentUnitId = row.EquipmentUnitId,
                                ManagementId = activeMgmt.Id,
                                CreatedDate = DateTime.UtcNow,
                                CreatedById = user?.Id
                            };
                            _context.Verifications.Add(verification);
                        }

                        verification.Date = DateTime.UtcNow;
                        verification.PhysicalCondition = row.Condition;
                        verification.Observations = row.Condition == PhysicalCondition.Bad ? row.Observations?.Trim() : null;
                        verification.Status = saveDraft
                            ? VerificationStatus.Draft
                            : row.Condition == PhysicalCondition.Bad ? VerificationStatus.WithObservations : VerificationStatus.Completed;
                        verification.LastModifiedDate = verification.Id == 0 ? null : DateTime.UtcNow;
                        if (verification.Id > 0) verification.ModifiedById = user?.Id;
                        await _context.SaveChangesAsync();

                        plan.VerificationId = verification.Id;

                        if (saveDraft)
                        {
                            MarkVerificationDraft(plan);
                            await _context.SaveChangesAsync();
                            created++;
                            continue;
                        }

                        if (row.Condition == PhysicalCondition.Bad)
                        {
                            badCount++;

                            var unit = await _context.EquipmentUnits.FindAsync(row.EquipmentUnitId)
                                ?? throw new InvalidOperationException("Uno de los equipos ya no existe.");
                            var obsText = row.Observations?.Trim() ?? "Equipo en mal estado requiere mantenimiento.";
                            var request = new Request
                            {
                                Type = RequestType.Technical,
                                ManagementId = activeMgmt.Id,
                                LaboratoryId = unit.LaboratoryId ?? throw new InvalidOperationException("Uno de los equipos ya no tiene laboratorio asignado."),
                                EquipmentId = unit.EquipmentId,
                                EquipmentUnitId = row.EquipmentUnitId,
                                Description = obsText.Length > 500 ? obsText[..497] + "..." : obsText,
                                Priority = RequestPriority.Medium,
                                Status = RequestStatus.Pending,
                                CreatedDate = DateTime.UtcNow,
                                CreatedById = user?.Id,
                                RequestedById = user?.Id
                            };

                            _context.Requests.Add(request);
                            await _context.SaveChangesAsync();

                            plan.RequestId = request.Id;
                            plan.CurrentPhase = WizardPhase.TechnicalRequest;
                            plan.CurrentState = WizardEquipmentState.AwaitingRequest;
                        }
                        else
                        {
                            plan.CurrentPhase = WizardPhase.Verification;
                            plan.CurrentState = WizardEquipmentState.VerifiedGood;
                        }

                        ClearDraft(plan);

                        await _context.SaveChangesAsync();
                        created++;
                    }
                    _context.ChangeTracker.Clear();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Falló el guardado masivo L-6 para gestión {ManagementId} y laboratorio {LabId}", activeMgmt.Id, LabId);
                TempData.Error("No se guardó la verificación: alguno de los equipos cambió o hubo un error de persistencia. Recargue e intente nuevamente.");
                return RedirectToPage(new { isWizard = IsWizard, labId = LabId, managementId = activeMgmt.Id });
            }

            if (saveDraft)
            {
                TempData.Success($"Borrador de la verificación guardado para {created} equipo(s).");
                if (IsWizard)
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = 1, SelectedLabId = LabId, ManagementId = activeMgmt.Id });
                return RedirectToPage("./Index");
            }

            TempData.Success($"{created} verificaciones registradas ({badCount} equipos con fallas).");

            if (IsWizard && badCount > 0)
                return RedirectToPage("/Index", new { ShowWizard = true, Step = 2, SelectedLabId = LabId, ManagementId = activeMgmt.Id });

            if (IsWizard)
                return RedirectToPage("/Index", new { ShowWizard = true, Step = 1, SelectedLabId = LabId, ManagementId = activeMgmt.Id });

            return RedirectToPage("./Index");
        }

        private static void MarkVerificationDraft(ManagementPlan plan)
        {
            plan.IsDraft = true;
            plan.DraftPhase = WizardPhase.Verification;
            plan.DraftSavedAt = DateTime.UtcNow;
            plan.DraftSummary = "Borrador de la verificación guardado (datos parciales).";
            plan.CurrentPhase = WizardPhase.Verification;
            plan.CurrentState = WizardEquipmentState.PendingVerification;
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
    }
}
