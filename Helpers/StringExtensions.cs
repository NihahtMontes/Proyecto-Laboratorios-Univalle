namespace Proyecto_Laboratorios_Univalle.Helpers
{
    public static class StringExtensions
    {
        /// <summary>
        /// Normaliza un string para comparación:
        /// - Trim de espacios
        /// - Múltiples espacios → espacio simple
        /// - ToLower para comparación case-insensitive
        /// </summary>
        public static string NormalizeComparison(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            // Regex más eficiente para espacios múltiples
            var trimmed = System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
            return trimmed.ToLowerInvariant();
        }

        /// <summary>
        /// Limpia espacios extra pero mantiene el case original
        /// </summary>
        public static string Clean(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Valida si el string contiene solo letras, espacios, acentos y guiones.
        /// Ideal para nombres de Países, Ciudades, etc.
        /// </summary>
        public static bool IsValidName(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Regex: Letras (incluyendo acentos), espacios y guiones
            var regex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s-]+$");
            return regex.IsMatch(value);
        }
    }
}
