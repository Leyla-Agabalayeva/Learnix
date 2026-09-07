namespace LMSFinal.Contracts.DTOs.Admin
{
    public record AdminCourseSearchRequest
    {
        public string? SearchTerm { get; init; }

        /// <summary>Draft / Published / Archived — без фильтра, если null.</summary>
        public string? Status { get; init; }

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 20;
    }
}
