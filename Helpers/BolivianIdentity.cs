using System.Text.RegularExpressions;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    /// <summary>
    /// Formatos bolivianos de cédula y teléfono. Validan la forma del dato; no confirman que exista en SEGIP o en una operadora.
    /// </summary>
    public static class BolivianIdentity
    {
        /// <summary>C.I.: 5 a 8 dígitos sin cero inicial y complemento opcional, por ejemplo 1234567-1A.</summary>
        public const string CiPattern = @"^[1-9]\d{4,7}(-\d[A-Za-z])?$";
        public const string CiError = "Formato de cédula inválido: usa de 5 a 8 dígitos y, si corresponde, el complemento (1234567-1A).";

        /// <summary>Teléfono: 8 dígitos, celular (6 o 7) o fijo (2, 3 o 4), con +591 opcional.</summary>
        public const string PhonePattern = @"^(\+?591[ -]?)?[23467]\d{7}$";
        public const string UserPhoneError = "Formato de teléfono inválido: usa 8 dígitos de Bolivia (celular 6 o 7, fijo 2, 3 o 4).";
        public const string PersonPhoneError = "Teléfono inválido: usa 8 dígitos de Bolivia (celular 6 o 7, fijo 2, 3 o 4).";

        public static bool IsValidCi(string? value) => value != null && Regex.IsMatch(value, CiPattern);

        public static bool IsValidPhone(string? value) => value != null && Regex.IsMatch(value, PhonePattern);

        /// <summary>Guarda solo los 8 dígitos nacionales; un valor vacío queda null y uno no válido se devuelve sin cambios.</summary>
        public static string? NormalizePhone(string? value)
        {
            var text = value?.Trim();
            if (string.IsNullOrEmpty(text)) return null;
            if (!IsValidPhone(text)) return text;
            var digits = new string(text.Where(char.IsDigit).ToArray());
            return digits[^8..];
        }
    }
}
