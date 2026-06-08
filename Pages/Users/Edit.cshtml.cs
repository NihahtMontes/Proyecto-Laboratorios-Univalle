using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EditModel> _logger;

        private const long MaxProfilePictureBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        public EditModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
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
            [Display(Name = "Nombres")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "El primer apellido es obligatorio")]
            [Display(Name = "Apellido Paterno")]
            public string LastName { get; set; } = string.Empty;

            [Display(Name = "Apellido Materno")]
            public string? SecondLastName { get; set; }

            [Required(ErrorMessage = "El documento de identidad es obligatorio")]
            [Display(Name = "C.I.")]
            public string IdentityCard { get; set; } = string.Empty;

            [Required(ErrorMessage = "El rol es obligatorio")]
            [Display(Name = "Rol")]
            public UserRole Role { get; set; }

            [Required]
            public GeneralStatus Status { get; set; }

            [Display(Name = "Cargo")]
            public string? Position { get; set; }

            [Display(Name = "Departamento")]
            public string? Department { get; set; }

            [Display(Name = "Fecha de Alta")]
            public DateTime? HireDate { get; set; }

            [Phone(ErrorMessage = "Formato de telefono invalido")]
            [Display(Name = "Telefono")]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "El correo institucional es obligatorio")]
            [EmailAddress(ErrorMessage = "Correo electronico invalido")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Usuario")]
            public string? UserName { get; set; }

            [DataType(DataType.Password)]
            [StringLength(100, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.", MinimumLength = 8)]
            [Display(Name = "Nueva Contrasena")]
            public string? NewPassword { get; set; }

            [Display(Name = "Foto de Perfil")]
            public IFormFile? ProfilePictureUpload { get; set; }

            public string? ExistingProfilePictureUrl { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null) return NotFound();

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

            ValidateProfilePicture(Input.ProfilePictureUpload);

            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            var normalizedEmail = Input.Email.Trim();
            var normalizedIdentityCard = Input.IdentityCard.Trim();

            var ciExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.IdentityCard == normalizedIdentityCard && u.Id != id && u.Status != GeneralStatus.Eliminado);

            if (ciExists)
            {
                ModelState.AddModelError("Input.IdentityCard", "Este C.I. ya esta asignado a otro usuario.");
            }

            var emailExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == normalizedEmail && u.Id != id && u.Status != GeneralStatus.Eliminado);

            if (emailExists)
            {
                ModelState.AddModelError("Input.Email", "Este correo electronico ya se encuentra registrado.");
            }

            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            var userToUpdate = await _context.Users
                .AsTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (userToUpdate == null)
            {
                return RedirectToPage("/Error", new
                {
                    module = "Usuarios",
                    entityId = id.ToString(),
                    message = "No se encontro el usuario solicitado para modificar.",
                    returnUrl = Url.Page("./Index"),
                    listUrl = Url.Page("./Index")
                });
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "users");
            var oldProfilePictureUrl = userToUpdate.ProfilePictureUrl;
            string? newProfilePictureUrl = null;
            string? newProfilePicturePath = null;

            try
            {
                ApplyInput(userToUpdate, normalizedEmail, normalizedIdentityCard);

                if (Input.ProfilePictureUpload is { Length: > 0 })
                {
                    Directory.CreateDirectory(uploadsFolder);
                    newProfilePictureUrl = BuildProfilePictureFileName(Input.ProfilePictureUpload);
                    newProfilePicturePath = Path.Combine(uploadsFolder, newProfilePictureUrl);

                    await using var fileStream = new FileStream(newProfilePicturePath, FileMode.CreateNew);
                    await Input.ProfilePictureUpload.CopyToAsync(fileStream);
                    userToUpdate.ProfilePictureUrl = newProfilePictureUrl;
                }

                if (!string.IsNullOrWhiteSpace(Input.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(userToUpdate);
                    var result = await _userManager.ResetPasswordAsync(userToUpdate, token, Input.NewPassword);
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("Input.NewPassword", error.Description);
                        }

                        DeleteUploadedFile(newProfilePicturePath);
                        Input.ExistingProfilePictureUrl = oldProfilePictureUrl;
                        LoadRoles();
                        return Page();
                    }
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    userToUpdate.ModifiedById = currentUser.Id;
                    userToUpdate.LastModifiedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                if (newProfilePictureUrl != null && !string.IsNullOrWhiteSpace(oldProfilePictureUrl))
                {
                    DeleteStoredProfilePicture(uploadsFolder, oldProfilePictureUrl);
                }

                if (currentUser?.Id == userToUpdate.Id)
                {
                    await _signInManager.RefreshSignInAsync(userToUpdate);
                }

                TempData.Success($"Datos de la cuenta '{userToUpdate.FullName}' actualizados correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                DeleteUploadedFile(newProfilePicturePath);
                if (!UserExists(id)) return NotFound();
                throw;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is DbUpdateException)
            {
                DeleteUploadedFile(newProfilePicturePath);
                _logger.LogError(ex, "No se pudo actualizar el usuario {UserId}.", id);
                Input.ExistingProfilePictureUrl = oldProfilePictureUrl;
                TempData.Error("No se pudo guardar la foto de perfil o los datos del usuario. Revise el archivo e intente nuevamente.");
                LoadRoles();
                return Page();
            }

            var returnUrlPost = HttpContext.Request.Query["returnUrl"].ToString();
            if (returnUrlPost == "Details")
                return RedirectToPage("./Details", new { id = userToUpdate.Id });

            return RedirectToPage("./Index");
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

        private void ApplyInput(User userToUpdate, string normalizedEmail, string normalizedIdentityCard)
        {
            userToUpdate.FirstName = Input.FirstName.Clean();
            userToUpdate.LastName = Input.LastName.Clean();
            userToUpdate.SecondLastName = Input.SecondLastName?.Clean();
            userToUpdate.IdentityCard = normalizedIdentityCard;
            userToUpdate.Role = Input.Role;
            userToUpdate.Status = Input.Status;
            userToUpdate.Position = Input.Position?.Clean();
            userToUpdate.Department = Input.Department?.Clean();
            userToUpdate.HireDate = Input.HireDate;
            userToUpdate.PhoneNumber = Input.PhoneNumber?.Trim();
            userToUpdate.Email = normalizedEmail;
            userToUpdate.NormalizedEmail = _userManager.NormalizeEmail(normalizedEmail);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        private async Task<string?> GetExistingProfilePictureUrlAsync(int id)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == id)
                .Select(u => u.ProfilePictureUrl)
                .FirstOrDefaultAsync();
        }

        private void ValidateProfilePicture(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return;
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImageExtensions.Contains(extension))
            {
                ModelState.AddModelError("Input.ProfilePictureUpload", "La foto debe ser JPG, PNG o WEBP.");
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Input.ProfilePictureUpload", "El archivo seleccionado no es una imagen valida.");
            }

            if (file.Length > MaxProfilePictureBytes)
            {
                ModelState.AddModelError("Input.ProfilePictureUpload", "La foto no puede superar los 5 MB.");
            }
        }

        private static string BuildProfilePictureFileName(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(invalidChar, '-');
            }

            baseName = string.IsNullOrWhiteSpace(baseName) ? "perfil" : baseName.Trim();
            return $"{Guid.NewGuid():N}_{baseName}{extension}";
        }

        private static void DeleteUploadedFile(string? filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        private static void DeleteStoredProfilePicture(string uploadsFolder, string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);
            var oldFilePath = Path.Combine(uploadsFolder, safeFileName);

            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }
        }

        private void LoadRoles()
        {
            ViewData["UserRole"] = EnumHelper.ToSelectList<UserRole>();
        }
    }
}
