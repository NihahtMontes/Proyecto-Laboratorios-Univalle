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
        public static string Normalize(this string? value)
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
    }
}