namespace GradProject.Application.DTOs.Common
{
    public class PagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
