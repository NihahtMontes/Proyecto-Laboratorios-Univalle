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
        /// Como <see cref="Clean"/>, pero conserva los saltos de línea: limpia los espacios de cada línea y
        /// quita las líneas vacías. Para textos con formato por líneas (p. ej. el checklist de 13 puntos del L-7).
        /// </summary>
        public static string CleanMultiline(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var lines = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
                .Select(line => System.Text.RegularExpressions.Regex.Replace(line.Trim(), @"\s+", " "))
                .Where(line => line.Length > 0);
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Valida si el string contiene solo letras, espacios, acentos y guiones.
        /// Ideal para nombres de Países, Ciudades, etc.
        /// </summary>
        public static bool IsValidName(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Regex: Letras (Unicode \p{L} incluye todos los acentos y diéresis), espacios y guiones
            var regex = new System.Text.RegularExpressions.Regex(@"^[\p{L}\s-]+$");
            return regex.IsMatch(value);
        }
    }
}
