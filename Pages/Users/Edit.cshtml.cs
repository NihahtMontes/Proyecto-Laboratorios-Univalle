using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment environment,
            ILogger<EditModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public UserInputModel Input { get; set; } = new();

        public int Id { get; set; }

        public class UserInputModel
        {
            [Required(ErrorMessage = "Los nombres son obligatorios")]
            [StringLength(100)]
            [Display(Name = "Nombres")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "El primer apellido es obligatorio")]
            [StringLength(100)]
            [Display(Name = "Apellido Paterno")]
            public string LastName { get; set; } = string.Empty;

            [StringLength(100)]
            [Display(Name = "Apellido Materno")]
            public string? SecondLastName { get; set; }

            [Required(ErrorMessage = "El documento de identidad es obligatorio")]
            [StringLength(10)]
            [RegularExpression(@"^[0-9A-Z-]*$", ErrorMessage = "Formato de cédula inválido")]
            [Display(Name = "C.I.")]
            public string IdentityCard { get; set; } = string.Empty;

            [Required(ErrorMessage = "El rol es obligatorio")]
            [Display(Name = "Rol")]
            public UserRole Role { get; set; }

            [Required]
            public GeneralStatus Status { get; set; }

            [StringLength(100)]
            [Display(Name = "Cargo")]
            public string? Position { get; set; }

            [StringLength(100)]
            [Display(Name = "Departamento")]
            public string? Department { get; set; }

            [Display(Name = "Fecha de Alta")]
            public DateTime? HireDate { get; set; }

            [Phone(ErrorMessage = "Formato de teléfono inválido")]
            [Display(Name = "Teléfono")]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "El correo institucional es obligatorio")]
            [EmailAddress(ErrorMessage = "Correo electrónico inválido")]
            [StringLength(256)]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Usuario")]
            public string? UserName { get; set; }

            [DataType(DataType.Password)]
            [StringLength(100, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.", MinimumLength = 8)]
            [Display(Name = "Nueva Contraseña")]
            public string? NewPassword { get; set; }

            [Display(Name = "Foto de Perfil")]
            public IFormFile? ProfilePictureUpload { get; set; }

            public string? ExistingProfilePictureUrl { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == id);

            if (user == null)
                return NotFound();

            if (user.Role == UserRole.SuperAdmin &&
                !User.IsInRole(AuthorizationHelper.RoleSuperAdmin))
            {
                return Forbid();
            }

            Id = user.Id;
            Input = MapToInput(user);
            ViewData["ReturnUrl"] = HttpContext.Request.Query["returnUrl"].ToString();
            LoadRoles();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Id = id;
            Input.ExistingProfilePictureUrl = await GetExistingProfilePictureUrlAsync(id);

            if (string.IsNullOrWhiteSpace(Input.NewPassword))
            {
                ModelState.Remove("Input.NewPassword");
            }

            if (!Enum.IsDefined(typeof(UserRole), Input.Role))
                ModelState.AddModelError("Input.Role", "Seleccione un rol válido.");

            if (!Enum.IsDefined(typeof(GeneralStatus), Input.Status))
                ModelState.AddModelError("Input.Status", "Seleccione un estado válido.");

            if (Input.Role == UserRole.SuperAdmin &&
                !User.IsInRole(AuthorizationHelper.RoleSuperAdmin))
            {
                ModelState.AddModelError("Input.Role", "Solo un superadministrador puede asignar este rol.");
            }

            var imageValidationError = await SafeImageUpload.ValidateAsync(
                Input.ProfilePictureUpload,
                HttpContext.RequestAborted);

            if (imageValidationError != null)
                ModelState.AddModelError("Input.ProfilePictureUpload", imageValidationError);

            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            var normalizedEmail = Input.Email.Trim().ToLowerInvariant();
            var normalizedIdentityCard = Input.IdentityCard.Trim().ToUpperInvariant();

            var ciExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(user => user.IdentityCard == normalizedIdentityCard &&
                    user.Id != id && user.Status != GeneralStatus.Eliminado);

            if (ciExists)
                ModelState.AddModelError("Input.IdentityCard", "Este C.I. ya está asignado a otro usuario.");

            var emailExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(user => user.Email != null && user.Email.ToLower() == normalizedEmail && user.Id != id);

            if (emailExists)
                ModelState.AddModelError("Input.Email", "Este correo electrónico ya se encuentra registrado.");

            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            var userToUpdate = await _context.Users
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync(user => user.Id == id);

            if (userToUpdate == null)
                return NotFound();

            if (userToUpdate.Role == UserRole.SuperAdmin &&
                !User.IsInRole(AuthorizationHelper.RoleSuperAdmin))
            {
                return Forbid();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == userToUpdate.Id &&
                (Input.Role != userToUpdate.Role || Input.Status != GeneralStatus.Activo))
            {
                ModelState.AddModelError(string.Empty,
                    "No puede cambiar su propio rol ni desactivar su propia cuenta.");
                LoadRoles();
                return Page();
            }

            if (userToUpdate.Role == UserRole.SuperAdmin &&
                (Input.Role != UserRole.SuperAdmin || Input.Status != GeneralStatus.Activo))
            {
                var hasAnotherActiveSuperAdmin = await _context.Users
                    .IgnoreQueryFilters()
                    .AnyAsync(user => user.Id != id &&
                        user.Role == UserRole.SuperAdmin &&
                        user.Status == GeneralStatus.Activo);

                if (!hasAnotherActiveSuperAdmin)
                {
                    ModelState.AddModelError(string.Empty,
                        "Debe existir al menos un superadministrador activo.");
                    LoadRoles();
                    return Page();
                }
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "users");
            var oldProfilePictureUrl = userToUpdate.ProfilePictureUrl;
            string? newProfilePictureUrl = null;
            string? newProfilePicturePath = null;

            try
            {
                if (Input.ProfilePictureUpload is { Length: > 0 })
                {
                    newProfilePictureUrl = await SafeImageUpload.SaveAsync(
                        Input.ProfilePictureUpload,
                        uploadsFolder,
                        HttpContext.RequestAborted);
                    newProfilePicturePath = Path.Combine(uploadsFolder, newProfilePictureUrl);
                }

                // Serializable bloquea lecturas/escrituras sobre el rango de SuperAdmin activos,
                // evitando que dos operaciones concurrentes degraden ambos al mismo tiempo.
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                // Re-verificación dentro de la transacción contra estado vigente de DB
                // (query fresca; no se confía en el snapshot cargado antes) para cerrar la carrera.
                var liveTarget = await _context.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(user => user.Id == id)
                    .Select(user => new { user.Role, user.Status })
                    .FirstOrDefaultAsync();

                if (liveTarget?.Role == UserRole.SuperAdmin &&
                    (Input.Role != UserRole.SuperAdmin || Input.Status != GeneralStatus.Activo))
                {
                    var hasAnotherActiveSuperAdmin = await _context.Users
                        .IgnoreQueryFilters()
                        .AnyAsync(user => user.Id != id &&
                            user.Role == UserRole.SuperAdmin &&
                            user.Status == GeneralStatus.Activo);

                    if (!hasAnotherActiveSuperAdmin)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Debe existir al menos un superadministrador activo.");
                        await transaction.RollbackAsync();
                        Input.ExistingProfilePictureUrl = oldProfilePictureUrl;
                        LoadRoles();
                        return Page();
                    }
                }

                var previousStatus = userToUpdate.Status;
                ApplyInput(userToUpdate, normalizedEmail, normalizedIdentityCard);

                if (newProfilePictureUrl != null)
                    userToUpdate.ProfilePictureUrl = newProfilePictureUrl;

                var roleResult = await _userManager.SynchronizeManagedRoleAsync(userToUpdate, Input.Role);
                if (!roleResult.Succeeded)
                {
                    AddIdentityErrors(roleResult, string.Empty);
                    await transaction.RollbackAsync();
                    SafeImageUpload.DeleteIfExists(newProfilePicturePath);
                    Input.ExistingProfilePictureUrl = oldProfilePictureUrl;
                    LoadRoles();
                    return Page();
                }

                if (!string.IsNullOrWhiteSpace(Input.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(userToUpdate);
                    var passwordResult = await _userManager.ResetPasswordAsync(userToUpdate, token, Input.NewPassword);
                    if (!passwordResult.Succeeded)
                    {
                        AddIdentityErrors(passwordResult, "Input.NewPassword");
                        await transaction.RollbackAsync();
                        SafeImageUpload.DeleteIfExists(newProfilePicturePath);
                        Input.ExistingProfilePictureUrl = oldProfilePictureUrl;
                        LoadRoles();
                        return Page();
                    }
                }

                if (Input.Status != GeneralStatus.Activo)
                {
                    var lockResult = await _userManager.SetLockoutEndDateAsync(userToUpdate, DateTimeOffset.MaxValue);
                    if (!lockResult.Succeeded)
                    {
                        AddIdentityErrors(lockResult, string.Empty);
                        await transaction.RollbackAsync();
                        SafeImageUpload.DeleteIfExists(newProfilePicturePath);
                        Input.ExistingProfilePictureUrl = oldProfilePictureUrl;
                        LoadRoles();
                        return Page();
                    }
                }
                else if (previousStatus != GeneralStatus.Activo)
                {
                    var unlockResult = await _userManager.SetLockoutEndDateAsync(userToUpdate, null);
                    if (!unlockResult.Succeeded)
                    {
                        AddIdentityErrors(unlockResult, string.Empty);
                        await transaction.RollbackAsync();
                        SafeImageUpload.DeleteIfExists(newProfilePicturePath);
                        Input.ExistingProfilePictureUrl = oldProfilePictureUrl;
                        LoadRoles();
                        return Page();
                    }

                    await _userManager.ResetAccessFailedCountAsync(userToUpdate);
                }

                var securityStampResult = await _userManager.UpdateSecurityStampAsync(userToUpdate);
                if (!securityStampResult.Succeeded)
                {
                    AddIdentityErrors(securityStampResult, string.Empty);
                    await transaction.RollbackAsync();
                    SafeImageUpload.DeleteIfExists(newProfilePicturePath);
                    Input.ExistingProfilePictureUrl = oldProfilePictureUrl;
                    LoadRoles();
                    return Page();
                }

                userToUpdate.ModifiedById = currentUser?.Id;
                userToUpdate.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (newProfilePictureUrl != null && !string.IsNullOrWhiteSpace(oldProfilePictureUrl))
                {
                    try
                    {
                        SafeImageUpload.DeleteStoredFile(uploadsFolder, oldProfilePictureUrl);
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        _logger.LogWarning(ex, "No se pudo retirar la foto anterior del usuario {UserId}.", id);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeImageUpload.DeleteIfExists(newProfilePicturePath);
                _logger.LogError(ex, "No se pudo actualizar el usuario {UserId}.", id);
                Input.ExistingProfilePictureUrl = oldProfilePictureUrl;
                TempData.Error("No se pudieron guardar los datos del usuario. Revise la información e intente nuevamente.");
                LoadRoles();
                return Page();
            }

            if (currentUser?.Id == userToUpdate.Id)
                await _signInManager.RefreshSignInAsync(userToUpdate);

            TempData.Success($"Datos de la cuenta '{userToUpdate.FullName}' actualizados correctamente.");

            var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
            return returnUrl == "Details"
                ? RedirectToPage("./Details", new { id = userToUpdate.Id })
                : RedirectToPage("./Index");
        }

        private static UserInputModel MapToInput(User user)
        {
            return new UserInputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                SecondLastName = user.SecondLastName,
                IdentityCard = user.IdentityCard,
                Role = user.Role,
                Status = user.Status,
                Position = user.Position,
                Department = user.Department,
                HireDate = user.HireDate,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName,
                ExistingProfilePictureUrl = user.ProfilePictureUrl
            };
        }

        private void ApplyInput(User user, string normalizedEmail, string normalizedIdentityCard)
        {
            user.FirstName = Input.FirstName.Clean();
            user.LastName = Input.LastName.Clean();
            user.SecondLastName = Input.SecondLastName?.Clean();
            user.IdentityCard = normalizedIdentityCard;
            user.Role = Input.Role;
            user.Status = Input.Status;
            user.Position = Input.Position?.Clean();
            user.Department = Input.Department?.Clean();
            user.HireDate = Input.HireDate;
            user.PhoneNumber = Input.PhoneNumber?.Trim();
            user.Email = normalizedEmail;
            user.NormalizedEmail = _userManager.NormalizeEmail(normalizedEmail);
        }

        private async Task<string?> GetExistingProfilePictureUrlAsync(int id)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .Where(user => user.Id == id)
                .Select(user => user.ProfilePictureUrl)
                .FirstOrDefaultAsync();
        }

        private void AddIdentityErrors(IdentityResult result, string key)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(key, error.Description);
            }
        }

        private void LoadRoles()
        {
            var roles = EnumHelper.ToSelectList<UserRole>();
            if (!User.IsInRole(AuthorizationHelper.RoleSuperAdmin))
            {
                roles.RemoveAll(item => item.Value == ((int)UserRole.SuperAdmin).ToString());
            }

            ViewData["UserRole"] = roles;
        }
    }
}
