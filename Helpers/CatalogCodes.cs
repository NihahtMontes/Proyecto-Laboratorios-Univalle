using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    /// <summary>
    /// Códigos automáticos de catálogos con formato PREFIJO-00001. El correlativo continúa desde el mayor
    /// número existente del prefijo (incluidos eliminados); los códigos históricos con otro formato se conservan.
    /// </summary>
    public static class CatalogCodes
    {
        public const string Faculty = "FAC";
        public const string Laboratory = "LAB";
        public const string Subject = "MAT";
        public const string CourseGroup = "GRP";
        /// <summary>Grupo de estudiantes (carrera + semestre + letra); GRP sigue siendo el grupo de cada materia.</summary>
        public const string StudentGroup = "GRU";
        /// <summary>PRC y no PRA: PRA-00001 ya identifica a las planificaciones (Plannings).</summary>
        public const string Practice = "PRC";
        public const string Article = "ART";
        public const string ArticleCategory = "CAT";
        public const string MeasurementUnit = "UNI";

        private const int MaxAttempts = 3;

        /// <summary>Siguiente código del prefijo. <paramref name="codes"/> debe incluir eliminados (IgnoreQueryFilters).</summary>
        public static async Task<string> NextAsync(IQueryable<string?> codes, string prefix)
        {
            var start = prefix + "-";
            var existing = await codes.Where(c => c != null && c.StartsWith(start)).ToListAsync();
            var max = existing
                .Select(c => c![start.Length..])
                .Where(suffix => suffix.Length is > 0 and <= 9 && suffix.All(char.IsAsciiDigit))
                .Select(int.Parse)
                .DefaultIfEmpty(0)
                .Max();
            return $"{start}{max + 1:00000}";
        }

        /// <summary>
        /// Asigna el siguiente código y guarda; si otro usuario tomó el mismo código a la vez (índice único),
        /// recalcula y reintenta. Otros errores se propagan al llamador.
        /// </summary>
        public static async Task SaveWithCodeAsync(this DbContext context, IQueryable<string?> codes, string prefix, Action<string> assign)
        {
            for (var attempt = 1; ; attempt++)
            {
                assign(await NextAsync(codes, prefix));
                try
                {
                    await context.SaveChangesAsync();
                    return;
                }
                catch (DbUpdateException ex) when (attempt < MaxAttempts && ex.InnerException is SqlException { Number: 2601 or 2627 })
                {
                }
            }
        }
    }
}
