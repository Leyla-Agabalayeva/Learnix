namespace LMSFinal.Contracts.DTOs.Admin
{
    public record AdminUserSearchRequest
    {
        public string? SearchTerm { get; init; }

        /// <summary>Admin / Instructor / Student — без фильтра, если null.</summary>
        public string? Role { get; init; }

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 20;
    }
}
