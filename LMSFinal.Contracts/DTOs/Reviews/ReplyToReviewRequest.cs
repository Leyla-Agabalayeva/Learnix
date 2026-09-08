namespace LMSFinal.Contracts.DTOs.Reviews
{
    public record ReplyToReviewRequest
    {
        public string Reply { get; init; } = string.Empty;
    }
}
