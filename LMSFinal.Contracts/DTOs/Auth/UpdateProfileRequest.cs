using System.ComponentModel.DataAnnotations;

namespace LMSFinal.Contracts.DTOs.Auth
{

    public record UpdateProfileRequest
    {
        [Required, MaxLength(100)]
        public string FirstName { get; init; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; init; } = string.Empty;

        [MaxLength(1000)]
        public string? Bio { get; init; }

        [MaxLength(500)]
        public string? AvatarUrl { get; init; }
    }
}
