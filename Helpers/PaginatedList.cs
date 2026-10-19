using Microsoft.EntityFrameworkCore;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = count;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source, int pageIndex, int pageSize = 20)
        {
            pageIndex = Math.Max(1, pageIndex);
            var count = await source.CountAsync();
            // Una página mayor que la última muestra la última (antes: tabla vacía sin explicación; QA H-005).
            var lastPage = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
            pageIndex = Math.Min(pageIndex, lastPage);
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}