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

        public MassCreateModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            UserManager<User> userManager,
            IManagementContextService managementContext)
        {
            _context = context;
            _userManager = userManager;
            _managementContext = managementContext;
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
            if (activeMgmt == null)
            {
                TempData.Error("No hay gestión activa.");
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            int created = 0;
            int badCount = 0;

            foreach (var row in Rows)
            {
                var plan = await _context.ManagementPlans
                    .AsTracking()
                    .FirstOrDefaultAsync(p => p.Id == row.PlanId);

                if (plan == null) continue;

                var verification = plan.VerificationId.HasValue
                    ? await _context.Verifications.AsTracking().FirstOrDefaultAsync(v => v.Id == plan.VerificationId.Value)
                    : null;

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

                    var unit = await _context.EquipmentUnits.FindAsync(row.EquipmentUnitId);
                    var obsText = row.Observations?.Trim() ?? "Equipo en mal estado requiere mantenimiento.";
                    var request = new Request
                    {
                        Type = RequestType.Technical,
                        ManagementId = activeMgmt.Id,
                        LaboratoryId = unit?.LaboratoryId ?? 0,
                        EquipmentId = unit?.EquipmentId ?? 0,
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

            if (saveDraft)
            {
                TempData.Success($"Borrador L-6 guardado para {created} equipo(s).");
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
            plan.DraftSummary = "Borrador L-6 guardado con verificación masiva parcial.";
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
