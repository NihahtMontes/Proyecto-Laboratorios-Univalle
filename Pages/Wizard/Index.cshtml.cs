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

namespace Proyecto_Laboratorios_Univalle.Pages.Wizard
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public int Step { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? CurrentEquipmentUnitId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int? CurrentVerificationId { get; set; }

        // Data for Step 1
        [BindProperty]
        public L6InputModel L6Input { get; set; } = new();

        public class L6InputModel
        {
            public int FacultyId { get; set; }
            public int LaboratoryId { get; set; }
            public int EquipmentUnitId { get; set; }
            public VerificationResult GeneralCondition { get; set; }
            public string? Observations { get; set; }
        }

        // Data for Step 2 (L7 Request)
        [BindProperty]
        public L7InputModel L7Input { get; set; } = new();

        public class L7InputModel
        {
            public string Description { get; set; } = string.Empty;
            public RequestPriority Priority { get; set; } = RequestPriority.Medium;
            public string? EstimatedRepairTime { get; set; }
        }

        public SelectList FacultiesList { get; set; }
        public SelectList LaboratoriesList { get; set; }
        public SelectList EquipmentUnitsList { get; set; }

        public void OnGet()
        {
            if (Step < 1 || Step > 6)
            {
                Step = 1;
            }
            
            LoadSelectLists();
        }

        public async Task<IActionResult> OnPostNextStepAsync()
        {
            if (Step == 1)
            {
                // Process L6 Verification form
                if (L6Input.EquipmentUnitId == 0)
                {
                    ModelState.AddModelError("L6Input.EquipmentUnitId", "Seleccione un equipo.");
                    LoadSelectLists();
                    return Page();
                }

                // Creación de la Solicitud L6
                var verification = new Verification
                {
                    EquipmentUnitId = L6Input.EquipmentUnitId,
                    Date = DateTime.UtcNow,
                    Status = VerificationStatus.Completed,
                    Observations = L6Input.Observations,
                    CreatedDate = DateTime.UtcNow
                };
                
                // Mapear el 'GeneralCondition' simple a los checks detallados
                if (L6Input.GeneralCondition == VerificationResult.Bad)
                {
                    verification.CriticalFindings = "Desperfecto detectado visualmente.";
                    verification.CablingCheck = VerificationResult.Bad; // Simular daño
                }

                var user = await _userManager.GetUserAsync(User);
                if (user != null) verification.CreatedById = user.Id;

                _context.Verifications.Add(verification);
                await _context.SaveChangesAsync();

                CurrentEquipmentUnitId = L6Input.EquipmentUnitId;
                CurrentVerificationId = verification.Id;
                
                // FILTRO DE L6 (M2.4): Si está todo bien, el wizard termina. 
                // Si hay desperfectos, pasa a L7 (Paso 2).
                if (L6Input.GeneralCondition == VerificationResult.Good || L6Input.GeneralCondition == VerificationResult.NotApplicable)
                {
                    TempData.Success("Verificación completada sin desperfectos. El flujo termina aquí.");
                    return RedirectToPage("/Index");
                }
                
                // Si hay daño, avanza a L7
                Step = 2;
            }
            else if (Step == 2)
            {
                if (CurrentEquipmentUnitId == null) return RedirectToPage("/Index");
                
                var unit = await _context.EquipmentUnits.Include(u => u.Equipment).FirstOrDefaultAsync(u => u.Id == CurrentEquipmentUnitId);
                if (unit == null) return NotFound();

                var request = new Request
                {
                    LaboratoryId = unit.LaboratoryId,
                    EquipmentId = unit.EquipmentId,
                    EquipmentUnitId = unit.Id,
                    Description = L7Input.Description,
                    Priority = L7Input.Priority,
                    EstimatedRepairTime = L7Input.EstimatedRepairTime,
                    Status = RequestStatus.Pending,
                    Type = RequestType.Technical,
                    CreatedDate = DateTime.UtcNow
                };

                var user = await _userManager.GetUserAsync(User);
                if (user != null) request.CreatedById = user.Id;

                _context.Requests.Add(request);
                await _context.SaveChangesAsync();
                
                // M2.3 retain parameters and jump to L8 Mantenimiento (Step 3)
                Step = 3;
            }
            else if (Step < 6)
            {
                Step++;
            }

            return RedirectToPage(new { step = Step, currentEquipmentUnitId = CurrentEquipmentUnitId, currentVerificationId = CurrentVerificationId });
        }

        public IActionResult OnPostPreviousStep()
        {
            if (Step > 1)
            {
                Step--;
            }
            return RedirectToPage(new { step = Step, currentEquipmentUnitId = CurrentEquipmentUnitId, currentVerificationId = CurrentVerificationId });
        }

        private void LoadSelectLists()
        {
            FacultiesList = new SelectList(_context.Faculties.Where(f => f.Status == GeneralStatus.Activo), "Id", "Name", L6Input?.FacultyId);
            
            if (L6Input?.FacultyId > 0)
            {
                LaboratoriesList = new SelectList(_context.Laboratories.Where(l => l.FacultyId == L6Input.FacultyId && l.Status == GeneralStatus.Activo), "Id", "Name", L6Input.LaboratoryId);
            }
            else
            {
                LaboratoriesList = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            if (L6Input?.LaboratoryId > 0)
            {
                EquipmentUnitsList = new SelectList(_context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == L6Input.LaboratoryId && u.CurrentStatus != EquipmentStatus.Deleted).Select(u => new { Id = u.Id, Name = u.Equipment.Name + " (" + u.InventoryNumber + ")" }), "Id", "Name", L6Input.EquipmentUnitId);
            }
            else
            {
                EquipmentUnitsList = new SelectList(_context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.CurrentStatus != EquipmentStatus.Deleted).OrderByDescending(u=>u.Id).Take(50).Select(u => new { Id = u.Id, Name = u.Equipment.Name + " (" + u.InventoryNumber + ")" }), "Id", "Name", L6Input?.EquipmentUnitId);
            }
        }
    }
}
