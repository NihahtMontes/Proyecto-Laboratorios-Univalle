using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services.StudentPractice;

namespace Proyecto_Laboratorios_Univalle.Pages;

/// <summary>
/// Registro propio de estudiantes (correo @est.univalle.edu). La cuenta nace inactiva y pendiente: un administrador
/// verifica el correo y los datos antes de que el estudiante pueda iniciar sesión y reservar práctica libre.
/// </summary>
[AllowAnonymous]
public class RegistroModel : PageModel
{
    private readonly UserManager<User> _users;
    private readonly RoleManager<IdentityRole<int>> _roles;
    private readonly ApplicationDbContext _db;
    private readonly StudentPracticeService _students;
    private readonly ILogger<RegistroModel> _logger;

    public RegistroModel(UserManager<User> users, RoleManager<IdentityRole<int>> roles, ApplicationDbContext db,
        StudentPracticeService students, ILogger<RegistroModel> logger)
    {
        _users = users;
        _roles = roles;
        _db = db;
        _students = students;
        _logger = logger;
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "El correo institucional es obligatorio.")]
        [EmailAddress(ErrorMessage = "Escriba un correo válido.")]
        [StringLength(100)]
        [Display(Name = "Correo institucional")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(100)]
        [Display(Name = "Nombres")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "El primer apellido es obligatorio.")]
        [StringLength(100)]
        [Display(Name = "Primer apellido")]
        public string LastName { get; set; } = "";

        [StringLength(100)]
        [Display(Name = "Segundo apellido")]
        public string? SecondLastName { get; set; }

        [Required(ErrorMessage = "Elija su carrera.")]
        [Display(Name = "Carrera")]
        public string Career { get; set; } = "";

        [Range(1, 10, ErrorMessage = "El semestre debe estar entre 1 y 10.")]
        [Display(Name = "Semestre")]
        public int Semester { get; set; } = 1;

        [Required(ErrorMessage = "El celular es obligatorio.")]
        [RegularExpression(BolivianIdentity.PhonePattern, ErrorMessage = "Escriba un celular de 8 dígitos (empieza en 6 o 7).")]
        [Display(Name = "Celular")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = "";

        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Repita la contraseña")]
        public string ConfirmPassword { get; set; } = "";
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public bool Sent { get; private set; }

    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? Redirect("/") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!StudentAccess.IsInstitutionalEmail(Input.Email))
            ModelState.AddModelError("Input.Email", $"Use su correo institucional de estudiante (@{StudentAccess.InstitutionalDomain}).");
        if (!StudentPracticeService.Careers.Contains(Input.Career))
            ModelState.AddModelError("Input.Career", "Elija una carrera de la lista.");
        if (!ModelState.IsValid) return Page();

        var email = Input.Email.Trim().ToLowerInvariant();
        if (await _users.FindByEmailAsync(email) != null || await _users.FindByNameAsync(email) != null)
        {
            ModelState.AddModelError("Input.Email", "Ya existe una cuenta con ese correo. Si la registró antes, espere la aprobación o inicie sesión.");
            return Page();
        }

        if (!await _roles.RoleExistsAsync(AuthorizationHelper.RoleStudent))
            await _roles.CreateAsync(new IdentityRole<int>(AuthorizationHelper.RoleStudent));

        // El usuario exige una C.I. única: el registro de estudiantes no la pide (decisión 2026-11-23), así que se usa
        // un identificador interno «E…» que no es una cédula.
        string identity;
        do identity = "E" + Convert.ToHexString(RandomNumberGenerator.GetBytes(5))[..9];
        while (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.IdentityCard == identity));

        var user = new User
        {
            UserName = email, Email = email, PhoneNumber = BolivianIdentity.NormalizePhone(Input.Phone.Trim()),
            FirstName = Input.FirstName.Trim(), LastName = Input.LastName.Trim(),
            SecondLastName = string.IsNullOrWhiteSpace(Input.SecondLastName) ? null : Input.SecondLastName.Trim(),
            IdentityCard = identity, Role = UserRole.Estudiante, Status = GeneralStatus.Inactivo,
            Position = "Estudiante", Department = Input.Career
        };
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var created = await _users.CreateAsync(user, Input.Password);
        if (!created.Succeeded)
        {
            foreach (var e in created.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return Page();
        }
        await _users.AddToRoleAsync(user, AuthorizationHelper.RoleStudent);
        var profile = await _students.AddProfileAsync(user.Id, Input.Career, Input.Semester);
        if (!profile.Ok)
        {
            ModelState.AddModelError(string.Empty, profile.Message);
            return Page();
        }
        await transaction.CommitAsync();
        _logger.LogInformation("Registro de estudiante {UserId} pendiente de aprobación", user.Id);
        Sent = true;
        return Page();
    }
}
