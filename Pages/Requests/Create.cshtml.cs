using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class CreateModel : PageModel
    {
        private const int TechnicalChecklistMinimumItems = 13;
        private const string TechnicalChecklistText = @"1. Desconexión del cable de la alimentación eléctrica para mantenimiento preventivo/correctivo 12 horas antes.
2. Limpieza y desinfección interna con productos no abrasivos.
3. Limpieza externa de condensador, serpentín, evaporador y retiro de polvo y grasas adheridas.
4. Verificación de presión del refrigerante.
5. Revisión de fugas y/o microfugas en serpentín.
6. Revisión de formaciones de hielo y condensaciones superficiales no esporádicas.
7. Control de temperatura y termostatos según norma.
8. Revisión de puertas y sellos de goma (empaques).
9. Limpieza de drenajes de deshielo.
10. Verificación del funcionamiento de ventiladores.
11. Mantenimiento eléctrico: inspección de cableado, terminales, protecciones eléctricas, etc.
12. Lubricación de partes móviles.
13. Mantenimiento con personal externo capacitado.";

        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementContext;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, 
            UserManager<User> userManager,
            IManagementContextService managementContext,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _managementContext = managementContext;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false)
        {
            // T0-10: ya no existe un L-7 "suelto": sin plan y sin gestión se enruta al
            // asistente correctivo (handler NewRequest del índice de solicitudes).
            if (!ManagementPlanId.HasValue && !ManagementId.HasValue)
            {
                return RedirectToPage("./Index", "NewRequest");
            }

            await LoadLists();

            var currentMgmt = await ResolveManagementAsync();
            var isCorrective = currentMgmt?.Type == ManagementType.Corrective;

            // T0-10: una gestión que no está activa no admite nuevas solicitudes de servicio técnico.
            if (ManagementId.HasValue && currentMgmt != null && currentMgmt.Status != ManagementStatus.Active)
            {
                TempData.Warning("La gestión seleccionada no está activa: no admite nuevas solicitudes de servicio técnico.");
                return RedirectToPage("./Index");
            }

            // T0-10: en preventivo la L-7 nace de un L-6 con falla; no se admite suelta.
            if (!ManagementPlanId.HasValue && currentMgmt?.Type == ManagementType.Preventive)
            {
                TempData.Warning("En el mantenimiento preventivo la solicitud nace de una verificación con falla: continúe desde el asistente preventivo.");
                return RedirectToPage("/Index", new { ShowWizard = true, Step = 2, ManagementId = currentMgmt.Id });
            }

            ViewData["IsCorrective"] = isCorrective;
            ViewData["ManagementId"] = currentMgmt?.Id;
            ViewData["ManagementType"] = currentMgmt?.Type.ToString();

            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.Verification).ThenInclude(v => v!.Faults)
                    .Include(p => p.TechnicalRequest)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);

                // R-02: L-7 solo puede registrarse si el plan está en la fase de solicitud técnica.
                var phaseRejection = WizardPhaseGuard.Validate(plan, WizardPhase.TechnicalRequest);
                if (phaseRejection != null)
                {
                    TempData.Warning(phaseRejection);
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = (int)plan!.CurrentPhase, ManagementId = plan.ManagementId });
                }

                ViewData["CurrentPhaseInt"] = (int)(plan?.CurrentPhase ?? WizardPhase.TechnicalRequest);

                if (plan?.Verification != null && !string.IsNullOrWhiteSpace(plan.Verification.Observations))
                {
                    Input.Description = plan.Verification.Observations;
                }

                if (plan?.TechnicalRequest != null)
                {
                    ApplyRequestToInput(plan.TechnicalRequest);
                }

                // Exponer IDs de fases previas para la sección de referencia vinculada
                if (plan?.VerificationId != null)
                {
                    ViewData["LinkedVerificationId"] = plan.VerificationId;
                }
                
                if (!equipmentUnitId.HasValue && plan?.EquipmentUnitId != null)
                {
                    equipmentUnitId = plan.EquipmentUnitId;
                }
            }

            if (equipmentUnitId.HasValue)
            {
                var unit = await _context.EquipmentUnits
                    .Include(u => u.Laboratory)
                    .Include(u => u.Equipment).ThenInclude(e => e!.Notes)
                    .FirstOrDefaultAsync(u => u.Id == equipmentUnitId.Value);

                if (unit != null)
                {
                    Input.EquipmentUnitId = unit.Id;
                    Input.LaboratoryId = unit.LaboratoryId ?? 0;
                    Input.FacultyId = unit.Laboratory?.FacultyId ?? 0;

                    ViewData["EquipmentNotes"] = unit.Equipment?.Notes?.Select(n => n.Note).ToList();
                    ViewData["CurrentEquipmentUnitId"] = unit.Id;

                    // Forzar carga de listas para que el Select2 muestre los valores
                    ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == Input.FacultyId).ToListAsync(), "Id", "Name", Input.LaboratoryId);
                    ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == Input.LaboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", Input.EquipmentUnitId);
                }
            }

            if (string.IsNullOrWhiteSpace(Input.Observations))
            {
                Input.Observations = TechnicalChecklistText;
            }


            ViewData["IsWizard"] = isWizard;
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        public class InputModel
        {
            public RequestType Type { get; set; } = RequestType.Technical;
            [Required(ErrorMessage = "La facultad es obligatoria")]
            public int FacultyId { get; set; }
            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            public int LaboratoryId { get; set; }
            [Required(ErrorMessage = "La unidad física es obligatoria")]
            public int EquipmentUnitId { get; set; }
            [Required(ErrorMessage = "La descripción del fallo es obligatoria")]
            [StringLength(1000, ErrorMessage = "La descripción de la falla no puede superar los 1000 caracteres.")]
            public string Description { get; set; } = string.Empty;
            public string? Observations { get; set; }
            public RequestPriority Priority { get; set; } = RequestPriority.Medium;
            [StringLength(100, ErrorMessage = "El tiempo estimado de reparación no puede superar los 100 caracteres.")]
            public string? EstimatedRepairTime { get; set; }
            public bool TechnicalChecklistConfirmed { get; set; }
        }

        public async Task<IActionResult> OnPostDraftAsync(bool isWizard = false)
        {
            return await SaveRequestAsync(isWizard, saveDraft: true);
        }

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            return await SaveRequestAsync(isWizard, saveDraft: false);
        }

        private async Task<IActionResult> SaveRequestAsync(bool isWizard, bool saveDraft)
        {
            // T0-10: sin plan y sin gestión no se crea nada: se enruta al asistente
            // correctivo mediante el handler NewRequest del índice de solicitudes.
            if (!ManagementPlanId.HasValue && !ManagementId.HasValue)
            {
                return RedirectToPage("./Index", "NewRequest");
            }

            ManagementPlan? wizardPlan = null;
            Management? currentMgmt = null;

            if (ManagementPlanId.HasValue)
            {
                wizardPlan = await _context.ManagementPlans
                    .AsTracking()
                    .Include(p => p.Management)
                    .Include(p => p.TechnicalRequest)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Laboratory)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Equipment)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);

                if (wizardPlan == null)
                {
                    TempData.Error("No se encontró el equipo en la gestión. Vuelva al asistente y ábralo de nuevo.");
                    await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                    ViewData["IsWizard"] = isWizard;
                    return Page();
                }

                // R-02: L-7 (definitivo o borrador) solo puede registrarse si el plan está en la
                // fase de solicitud técnica; en otra fase no se guarda nada para no saltar fases
                // ni mover el wizard hacia atrás.
                var phaseRejection = WizardPhaseGuard.Validate(wizardPlan, WizardPhase.TechnicalRequest);
                if (phaseRejection != null)
                {
                    TempData.Warning(phaseRejection);
                    if (isWizard)
                        return RedirectToPage("/Index", new { ShowWizard = true, Step = (int)wizardPlan.CurrentPhase, ManagementId = wizardPlan.ManagementId });

                    await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                    ViewData["IsWizard"] = isWizard;
                    return Page();
                }

                currentMgmt = wizardPlan.Management;

                if (wizardPlan.EquipmentUnitId.HasValue)
                {
                    Input.EquipmentUnitId = wizardPlan.EquipmentUnitId.Value;
                    Input.LaboratoryId = wizardPlan.EquipmentUnit?.LaboratoryId ?? Input.LaboratoryId;
                    Input.FacultyId = wizardPlan.EquipmentUnit?.Laboratory?.FacultyId ?? Input.FacultyId;
                }

                ModelState.Remove("Input.FacultyId");
                ModelState.Remove("Input.LaboratoryId");
                ModelState.Remove("Input.EquipmentUnitId");
            }

            currentMgmt ??= isWizard && !ManagementId.HasValue && !ManagementPlanId.HasValue
                ? null
                : await ResolveManagementAsync();
            var isCorrective = currentMgmt?.Type == ManagementType.Corrective;

            ViewData["IsWizard"] = isWizard;
            ViewData["IsCorrective"] = isCorrective;
            ViewData["ManagementId"] = currentMgmt?.Id ?? ManagementId;
            ViewData["ManagementType"] = currentMgmt?.Type.ToString();
            ViewData["CurrentPhaseInt"] = (int)(wizardPlan?.CurrentPhase ?? WizardPhase.TechnicalRequest);

            // T0-10: una gestión que no está activa no admite nuevas solicitudes de servicio técnico.
            if (ManagementId.HasValue && currentMgmt != null && currentMgmt.Status != ManagementStatus.Active)
            {
                TempData.Warning("La gestión seleccionada no está activa: no admite nuevas solicitudes de servicio técnico.");
                return RedirectToPage("./Index");
            }

            // T0-10: en preventivo la L-7 nace de un L-6 con falla del asistente; no se
            // guarda una L-7 suelta (sin ManagementPlanId) en una gestión preventiva.
            if (wizardPlan == null && currentMgmt?.Type == ManagementType.Preventive)
            {
                TempData.Warning("En el mantenimiento preventivo la solicitud nace de una verificación con falla: continúe desde el asistente preventivo.");
                return RedirectToPage("/Index", new { ShowWizard = true, Step = 2, ManagementId = currentMgmt.Id });
            }

            if (saveDraft)
            {
                ModelState.Remove("Input.Description");
            }
            else
            {
                if (!Enum.IsDefined(typeof(RequestPriority), Input.Priority) || Input.Priority == RequestPriority.PorConfirmar)
                {
                    ModelState.AddModelError("Input.Priority", "Seleccione una prioridad válida para la solicitud.");
                }

                if (!Input.TechnicalChecklistConfirmed || CountChecklistItems(Input.Observations) < TechnicalChecklistMinimumItems)
                {
                    ModelState.AddModelError("Input.TechnicalChecklistConfirmed", "Debe revisar y confirmar el checklist técnico obligatorio de 13 puntos.");
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value?.Errors.Count > 0)
                    .SelectMany(ms => ms.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? $"{ms.Key}: valor inválido." : e.ErrorMessage))
                    .Distinct()
                    .Take(4)
                    .ToList();

                TempData.Error(errors.Count > 0
                    ? "No se pudo registrar la solicitud de servicio técnico: " + string.Join(" ", errors)
                    : "No se pudo registrar la solicitud de servicio técnico. Revise los campos obligatorios.");

                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            if (currentMgmt == null)
            {
                TempData.Warning("No se ha detectado una gestión activa. Debe activar un periodo de gestión para registrar solicitudes.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            var unit = await _context.EquipmentUnits
                .AsTracking()
                .FirstOrDefaultAsync(u => u.Id == Input.EquipmentUnitId);

            if (unit == null)
            {
                TempData.Error("No se pudo registrar la solicitud de servicio técnico: la unidad física no existe.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            if (unit.LaboratoryId == null)
            {
                TempData.Error("No se pudo registrar la solicitud de servicio técnico: la unidad física no tiene laboratorio asignado.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            var currentRequestId = wizardPlan?.RequestId ?? wizardPlan?.TechnicalRequest?.Id ?? 0;
            var hasPendingRequest = currentMgmt.Type != ManagementType.Corrective && await _context.Requests
                .AsNoTracking()
                .AnyAsync(r => r.Type == RequestType.Technical
                    && r.ManagementId == currentMgmt.Id
                    && r.EquipmentUnitId == Input.EquipmentUnitId
                    && r.Status == RequestStatus.Pending
                    && r.Id != currentRequestId);

            if (hasPendingRequest)
            {
                TempData.Warning("Ya existe una solicitud de servicio técnico pendiente para esta unidad en la gestión seleccionada.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            // T0-09 (D-7): en correctivo solo puede existir UN proceso abierto por unidad y
            // gestión. Antes de guardar nada (incluso borrador) se verifica que la unidad
            // no tenga ya un plan no completado en la gestión correctiva activa.
            if (currentMgmt.Type == ManagementType.Corrective && wizardPlan == null)
            {
                var openCorrectivePlan = await _context.ManagementPlans
                    .AsNoTracking()
                    .Where(p => p.ManagementId == currentMgmt.Id
                        && p.EquipmentUnitId == Input.EquipmentUnitId
                        && p.PlanStatus != ManagementPlanStatus.Completed)
                    .OrderByDescending(p => p.CurrentPhase)
                    .FirstOrDefaultAsync();

                if (openCorrectivePlan != null)
                {
                    TempData.Warning($"La unidad {unit.InventoryNumber} ya tiene un proceso correctivo abierto en esta gestión. Continúelo desde el asistente (paso {(int)openCorrectivePlan.CurrentPhase}).");
                    return RedirectToPage("/Index", new
                    {
                        ShowWizard = true,
                        Step = (int)openCorrectivePlan.CurrentPhase,
                        ManagementId = currentMgmt.Id,
                        FocusPlanId = openCorrectivePlan.Id
                    });
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = wizardPlan?.TechnicalRequest ?? new Request
                {
                    CreatedDate = DateTime.UtcNow
                };

                request.RequestDate ??= request.CreatedDate.Date;
                request.Type = RequestType.Technical;
                request.ManagementId = currentMgmt.Id;
                request.LaboratoryId = Input.LaboratoryId;
                request.EquipmentId = unit.EquipmentId;
                request.EquipmentUnitId = Input.EquipmentUnitId;
                request.Description = string.IsNullOrWhiteSpace(Input.Description)
                    ? "Borrador de la solicitud pendiente de descripción técnica."
                    : Input.Description.Clean()!;
                request.Priority = Input.Priority;
                // T0-03b: el checklist técnico completo (~900 caracteres) ya no se trunca a 500:
                // Observations admite 2000 caracteres y solo se recorta pasando ese límite.
                // CleanMultiline conserva los saltos de línea: con Clean() el checklist quedaba en una sola línea y al
                // reabrir el borrador CountChecklistItems contaba 1 punto en vez de 13 (R-03).
                var observations = Input.Observations.CleanMultiline();
                request.Observations = observations.Length > 2000 ? observations[..1997] + "..." : (observations.Length == 0 ? null : observations);
                request.EstimatedRepairTime = Input.EstimatedRepairTime?.Clean();
                request.Status = RequestStatus.Pending;
                request.LastModifiedDate = request.Id == 0 ? null : DateTime.UtcNow;

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    if (request.Id == 0)
                    {
                        request.CreatedById = currentUser.Id;
                        request.RequestedById = currentUser.Id;
                    }
                    else
                    {
                        request.ModifiedById = currentUser.Id;
                    }
                }

                if (request.Id == 0)
                {
                    _context.Requests.Add(request);
                }
                await _context.SaveChangesAsync();

                if (wizardPlan != null)
                {
                    wizardPlan.RequestId = request.Id;
                    if (saveDraft)
                    {
                        MarkRequestDraft(wizardPlan);
                    }
                    else
                    {
                        wizardPlan.CurrentPhase = WizardPhase.Maintenance;
                        wizardPlan.CurrentState = WizardEquipmentState.AwaitingMaintenance;
                        ClearDraft(wizardPlan);
                    }
                    await _context.SaveChangesAsync();
                }
                else if (currentMgmt.Type == ManagementType.Corrective)
                {
                    wizardPlan = new ManagementPlan
                    {
                        ManagementId = currentMgmt.Id,
                        EquipmentUnitId = Input.EquipmentUnitId,
                        CurrentPhase = saveDraft ? WizardPhase.TechnicalRequest : WizardPhase.Maintenance,
                        CurrentState = saveDraft ? WizardEquipmentState.AwaitingRequest : WizardEquipmentState.AwaitingMaintenance,
                        PlanStatus = ManagementPlanStatus.InProgress,
                        RequestId = request.Id
                    };
                    if (saveDraft)
                    {
                        MarkRequestDraft(wizardPlan);
                    }
                    _context.ManagementPlans.Add(wizardPlan);
                    await _context.SaveChangesAsync();
                }
                else if (isWizard)
                {
                    throw new InvalidOperationException("Abra la solicitud desde el asistente preventivo para que el equipo pase al registro de mantenimiento.");
                }

                await transaction.CommitAsync();

                TempData.Success(saveDraft ? "Borrador de la solicitud de servicio técnico guardado correctamente." : "Solicitud de servicio técnico registrada correctamente.");

                if (isWizard)
                {
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = saveDraft ? 2 : 3, SelectedLabId = Input.LaboratoryId, ManagementId = currentMgmt.Id });
                }

                return RedirectToPage("./Index");
            }
            // T0-09 (D-7, respaldo ante concurrencia): si otro usuario registró casi al
            // mismo tiempo una solicitud correctiva para la misma unidad, SQL Server
            // rechaza el duplicado por IX_ManagementPlans_ManagementId_EquipmentUnitId.
            // Se responde con el mismo mensaje claro en lugar del error genérico.
            catch (DbUpdateException ex) when (IsUniqueOpenCorrectivePlanConflict(ex))
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex,
                    "Conflicto de unicidad: la unidad {EquipmentUnitId} ya tenía un proceso correctivo abierto en la gestión {ManagementId}.",
                    Input.EquipmentUnitId,
                    currentMgmt?.Id);
                TempData.Warning($"La unidad {unit.InventoryNumber} ya tiene un proceso correctivo abierto en esta gestión. Continúelo desde el asistente.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(
                    ex,
                    "Error al registrar solicitud L-7 para unidad {EquipmentUnitId} y plan {ManagementPlanId}.",
                    Input.EquipmentUnitId,
                    ManagementPlanId);
                TempData.Error("No se pudo registrar la solicitud de servicio técnico. Intente nuevamente.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }
        }

        private void ApplyRequestToInput(Request request)
        {
            Input.Description = request.Description == "Borrador de la solicitud pendiente de descripción técnica." ? string.Empty : request.Description;
            Input.Observations = request.Observations;
            // T0-03b: los borradores guardados con el límite antiguo (500) dejaron un
            // prefijo truncado del checklist; al reabrirlos se restaura el texto completo.
            if (IsTruncatedChecklistPrefix(request.Observations))
            {
                Input.Observations = TechnicalChecklistText;
            }
            Input.Priority = request.Priority;
            Input.EstimatedRepairTime = request.EstimatedRepairTime;
        }

        /// <summary>
        /// Detecta un Observations que es un prefijo truncado del checklist técnico:
        /// empieza igual que el texto completo, termina en "..." y es más corto que él.
        /// </summary>
        private static bool IsTruncatedChecklistPrefix(string? observations)
        {
            if (string.IsNullOrWhiteSpace(observations))
                return false;

            // Se compara sin saltos de línea: los borradores antiguos se guardaron con Clean(), que los colapsaba.
            var stored = observations.Clean();
            if (stored.EndsWith("...", StringComparison.Ordinal))
                stored = stored[..^3].TrimEnd();
            var full = TechnicalChecklistText.Clean();
            return stored.Length > 0
                && full.StartsWith(stored, StringComparison.Ordinal)
                && CountChecklistItems(observations) < TechnicalChecklistMinimumItems;
        }

        /// <summary>
        /// DbUpdateException por violación del índice único de procesos correctivos
        /// abiertos por unidad y gestión (SQL Server 2601/2627 sobre IX_ManagementPlans).
        /// </summary>
        private static bool IsUniqueOpenCorrectivePlanConflict(DbUpdateException ex)
        {
            return ex.InnerException is SqlException { Number: 2601 or 2627 } sqlError
                && sqlError.Message.Contains("IX_ManagementPlans", StringComparison.OrdinalIgnoreCase);
        }

        private static int CountChecklistItems(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Count(line => char.IsDigit(line.TrimStart().FirstOrDefault()));
        }

        private static void MarkRequestDraft(ManagementPlan plan)
        {
            plan.IsDraft = true;
            plan.DraftPhase = WizardPhase.TechnicalRequest;
            plan.DraftSavedAt = DateTime.UtcNow;
            plan.DraftSummary = "Borrador de la solicitud de servicio técnico guardado (datos parciales).";
            plan.CurrentPhase = WizardPhase.TechnicalRequest;
            plan.CurrentState = WizardEquipmentState.AwaitingRequest;
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

        private async Task LoadLists(int facultyId = 0, int labId = 0, int equipmentUnitId = 0)
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name)
                .ToListAsync(), "Id", "Name", facultyId);

            if (facultyId > 0)
            {
                ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories
                    .Where(l => l.FacultyId == facultyId)
                    .OrderBy(l => l.Name)
                    .ToListAsync(), "Id", "Name", labId);
            }
            else
            {
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            if (labId > 0)
            {
                ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Where(u => u.LaboratoryId == labId)
                    .Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" })
                    .ToListAsync(), "Id", "Name", equipmentUnitId);
            }
            else
            {
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        // Handlers para AJAX (Asegúrate de que existan en tu controlador o aquí)
        public async Task<JsonResult> OnGetLaboratoriesByFaculty(int facultyId)
        {
            var labs = await _context.Laboratories.Where(l => l.FacultyId == facultyId).Select(l => new { id = l.Id, name = l.Name }).ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<JsonResult> OnGetUnitsByLab(int laboratoryId)
        {
            var units = await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == laboratoryId).Select(u => new { id = u.Id, name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync();
            return new JsonResult(units);
        }

        // Buscador global de equipo (solo correctivo): busca en todos los laboratorios
        // por nombre del equipo o número de inventario. Máximo 20 resultados.
        public async Task<JsonResult> OnGetSearchUnitsAsync(string? term)
        {
            term = term?.Trim();
            if (string.IsNullOrEmpty(term) || term.Length < 2)
                return new JsonResult(new { results = Array.Empty<object>() });

            var units = await _context.EquipmentUnits
                .AsNoTracking()
                .Where(u => u.CurrentStatus != EquipmentStatus.Deleted
                            && u.LaboratoryId != null
                            && u.Laboratory != null
                            && (u.InventoryNumber.Contains(term)
                                || (u.Equipment != null && u.Equipment.Name.Contains(term))))
                .OrderBy(u => u.Equipment!.Name)
                .ThenBy(u => u.InventoryNumber)
                .Select(u => new
                {
                    id = u.Id,
                    equipmentName = u.Equipment!.Name,
                    inventoryNumber = u.InventoryNumber,
                    laboratoryName = u.Laboratory!.Name,
                    laboratoryId = u.LaboratoryId!.Value,
                    facultyId = u.Laboratory!.FacultyId
                })
                .Take(20)
                .ToListAsync(HttpContext.RequestAborted);

            var results = units.Select(u => new
            {
                u.id,
                text = $"{u.equipmentName} ({u.inventoryNumber}) — {u.laboratoryName}",
                u.facultyId,
                u.laboratoryId
            });

            return new JsonResult(new { results });
        }

        public async Task<JsonResult> OnGetKardexDetailAsync(int equipmentId)
        {
            var summary = await EquipmentKardexSummaryBuilder.BuildAsync(_context, equipmentId, HttpContext.RequestAborted);
            return summary == null
                ? new JsonResult(new { error = "No encontrado" }) { StatusCode = StatusCodes.Status404NotFound }
                : new JsonResult(summary);
        }

        private async Task<Management?> ResolveManagementAsync()
        {
            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
                if (plan != null)
                    return await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == plan.ManagementId);
            }
            if (ManagementId.HasValue)
                return await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == ManagementId.Value);
            return await _managementContext.GetCurrentManagementAsync();
        }
    }
}
