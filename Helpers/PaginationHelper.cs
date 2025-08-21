// Helpers/PaginationHelper.cs
using WuanTech.API.DTOs;

namespace WuanTech.API.Helpers
{
    public static class PaginationHelper
    {
        public static PagedResult<T> CreatePagedResult<T>(
            List<T> items,
            int totalCount,
            int page,
            int pageSize)
        {
            return new PagedResult<T>(items, totalCount, page, pageSize)
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

        }

        public static (int skip, int take) CalculateSkipTake(int page, int pageSize)
        {
            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = Math.Max(1, Math.Min(pageSize, 100)); // Max 100 items per page

            var skip = (normalizedPage - 1) * normalizedPageSize;
            return (skip, normalizedPageSize);
        }

        public static string GeneratePaginationHeader(int page, int pageSize, int totalCount, string baseUrl)
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var links = new List<string>();

            if (page > 1)
            {
                links.Add($"<{baseUrl}?page={page - 1}&pageSize={pageSize}>; rel=\"prev\"");
                links.Add($"<{baseUrl}?page=1&pageSize={pageSize}>; rel=\"first\"");
            }

            if (page < totalPages)
            {
                links.Add($"<{baseUrl}?page={page + 1}&pageSize={pageSize}>; rel=\"next\"");
                links.Add($"<{baseUrl}?page={totalPages}&pageSize={pageSize}>; rel=\"last\"");
            }

            return string.Join(", ", links);
        }
    }
}