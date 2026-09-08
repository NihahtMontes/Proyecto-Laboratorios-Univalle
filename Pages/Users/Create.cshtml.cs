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

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            UserManager<User> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<CreateModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
            [StringLength(256)]
            [Display(Name = "Usuario")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "El correo institucional es obligatorio")]
            [EmailAddress(ErrorMessage = "Correo electrónico inválido")]
            [StringLength(256)]
            [Display(Name = "Correo")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Los nombres son obligatorios")]
            [StringLength(100)]
            [Display(Name = "Nombres")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "El primer apellido es obligatorio")]
            [StringLength(100)]
            [Display(Name = "Apellido")]
            public string LastName { get; set; } = string.Empty;

            [StringLength(100)]
            [Display(Name = "Segundo Apellido")]
            public string? SecondLastName { get; set; }

            [Required(ErrorMessage = "El documento de identidad es obligatorio")]
            [StringLength(10)]
            [RegularExpression(@"^[0-9A-Z-]*$", ErrorMessage = "Formato de cédula inválido")]
            [Display(Name = "C.I.")]
            public string IdentityCard { get; set; } = string.Empty;

            [Required(ErrorMessage = "El rol es obligatorio")]
            [Display(Name = "Rol")]
            public UserRole Role { get; set; }

            [StringLength(100)]
            [Display(Name = "Cargo")]
            public string? Position { get; set; }

            [StringLength(100)]
            [Display(Name = "Departamento")]
            public string? Department { get; set; }

            [Display(Name = "Fecha de Contratación")]
            public DateTime? HireDate { get; set; }

            [Required(ErrorMessage = "El teléfono es obligatorio")]
            [Phone(ErrorMessage = "Formato de teléfono inválido")]
            [Display(Name = "Teléfono")]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria")]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Foto de Perfil")]
            public IFormFile? ProfilePictureUpload { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IActionResult OnGet()
        {
            LoadRoles();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!Enum.IsDefined(typeof(UserRole), Input.Role))
            {
                ModelState.AddModelError("Input.Role", "Seleccione un rol válido.");
            }

            if (Input.Role == UserRole.SuperAdmin &&
                !User.IsInRole(AuthorizationHelper.RoleSuperAdmin))
            {
                ModelState.AddModelError("Input.Role", "Solo un superadministrador puede asignar este rol.");
            }

            var imageValidationError = await SafeImageUpload.ValidateAsync(
                Input.ProfilePictureUpload,
                HttpContext.RequestAborted);

            if (imageValidationError != null)
            {
                ModelState.AddModelError("Input.ProfilePictureUpload", imageValidationError);
            }

            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            var normalizedCI = Input.IdentityCard.Trim().ToUpperInvariant();
            var normalizedEmail = Input.Email.Trim().ToLowerInvariant();
            var normalizedUserName = Input.UserName.Trim().ToLowerInvariant();

            var ciExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.IdentityCard.Trim() == normalizedCI && u.Status != GeneralStatus.Eliminado);

            if (ciExists)
                ModelState.AddModelError("Input.IdentityCard", "Este documento de identidad ya está vinculado a otra cuenta.");

            var emailExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email != null && u.Email.Trim().ToLower() == normalizedEmail);

            if (emailExists)
                ModelState.AddModelError("Input.Email", "Este correo electrónico ya se encuentra registrado.");

            var userNameExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.UserName != null && u.UserName.Trim().ToLower() == normalizedUserName);

            if (userNameExists)
                ModelState.AddModelError("Input.UserName", "El nombre de usuario ya está en uso.");

            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            var user = new User
            {
                UserName = normalizedUserName,
                Email = normalizedEmail,
                FirstName = Input.FirstName.Clean(),
                LastName = Input.LastName.Clean(),
                SecondLastName = Input.SecondLastName?.Clean(),
                IdentityCard = normalizedCI,
                Role = Input.Role,
                Position = Input.Position?.Clean(),
                Department = Input.Department?.Clean(),
                HireDate = Input.HireDate ?? DateTime.UtcNow,
                PhoneNumber = Input.PhoneNumber?.Trim(),
                Status = GeneralStatus.Activo,
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var currentUser = await _userManager.GetUserAsync(User);
            user.CreatedById = currentUser?.Id;

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "users");
            string? uploadedFilePath = null;

            try
            {
                if (Input.ProfilePictureUpload is { Length: > 0 })
                {
                    var storedFileName = await SafeImageUpload.SaveAsync(
                        Input.ProfilePictureUpload,
                        uploadsFolder,
                        HttpContext.RequestAborted);
                    user.ProfilePictureUrl = storedFileName;
                    uploadedFilePath = Path.Combine(uploadsFolder, storedFileName);
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                var createResult = await _userManager.CreateAsync(user, Input.Password);
                if (!createResult.Succeeded)
                {
                    AddIdentityErrors(createResult);
                    await transaction.RollbackAsync();
                    SafeImageUpload.DeleteIfExists(uploadedFilePath);
                    LoadRoles();
                    return Page();
                }

                var roleResult = await _userManager.SynchronizeManagedRoleAsync(user, Input.Role);
                if (!roleResult.Succeeded)
                {
                    AddIdentityErrors(roleResult);
                    await transaction.RollbackAsync();
                    SafeImageUpload.DeleteIfExists(uploadedFilePath);
                    LoadRoles();
                    return Page();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                SafeImageUpload.DeleteIfExists(uploadedFilePath);
                _logger.LogError(ex, "No se pudo crear la cuenta {UserName}.", normalizedUserName);
                ModelState.AddModelError(string.Empty,
                    "No se pudo crear la cuenta. Revise los datos e intente nuevamente.");
                LoadRoles();
                return Page();
            }

            TempData.Success($"Cuenta de usuario para '{user.FullName}' creada exitosamente.");
            return RedirectToPage("./Index");
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
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
