namespace LMSFinal.Contracts.DTOs.Auth
{
    public record ForgotPasswordRequest
    {
        public string Email { get; init; } = string.Empty;
    }
}
