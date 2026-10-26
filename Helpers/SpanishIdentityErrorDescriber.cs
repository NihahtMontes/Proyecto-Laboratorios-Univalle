using Microsoft.AspNetCore.Identity;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    /// <summary>
    /// Mensajes de ASP.NET Core Identity en español (antes se mostraban en inglés, p. ej. "Passwords must be at least 12 characters.").
    /// </summary>
    public sealed class SpanishIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() => Error(nameof(DefaultError), "Ocurrió un error desconocido.");
        public override IdentityError ConcurrencyFailure() => Error(nameof(ConcurrencyFailure), "Otro usuario modificó este registro. Recargue la página e intente nuevamente.");
        public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "La contraseña actual es incorrecta.");
        public override IdentityError InvalidToken() => Error(nameof(InvalidToken), "El código de seguridad no es válido o venció.");
        public override IdentityError LoginAlreadyAssociated() => Error(nameof(LoginAlreadyAssociated), "Ya existe una cuenta asociada a este inicio de sesión.");
        public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), $"El nombre de usuario '{userName}' no es válido: solo admite letras, números y los símbolos - . _ @ +");
        public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), $"El correo '{email}' no es válido.");
        public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), $"El nombre de usuario '{userName}' ya está en uso.");
        public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), $"El correo '{email}' ya se encuentra registrado.");
        public override IdentityError InvalidRoleName(string? role) => Error(nameof(InvalidRoleName), $"El rol '{role}' no es válido.");
        public override IdentityError DuplicateRoleName(string role) => Error(nameof(DuplicateRoleName), $"El rol '{role}' ya existe.");
        public override IdentityError UserAlreadyHasPassword() => Error(nameof(UserAlreadyHasPassword), "El usuario ya tiene una contraseña.");
        public override IdentityError UserLockoutNotEnabled() => Error(nameof(UserLockoutNotEnabled), "El bloqueo de cuenta no está habilitado para este usuario.");
        public override IdentityError UserAlreadyInRole(string role) => Error(nameof(UserAlreadyInRole), $"El usuario ya tiene el rol '{role}'.");
        public override IdentityError UserNotInRole(string role) => Error(nameof(UserNotInRole), $"El usuario no tiene el rol '{role}'.");
        public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"La contraseña debe tener al menos {length} caracteres.");
        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error(nameof(PasswordRequiresUniqueChars), $"La contraseña debe tener al menos {uniqueChars} caracteres distintos.");
        public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "La contraseña debe incluir al menos un símbolo (por ejemplo ! # $ %).");
        public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "La contraseña debe incluir al menos un número (0-9).");
        public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "La contraseña debe incluir al menos una letra minúscula (a-z).");
        public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "La contraseña debe incluir al menos una letra mayúscula (A-Z).");

        private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
    }
}
