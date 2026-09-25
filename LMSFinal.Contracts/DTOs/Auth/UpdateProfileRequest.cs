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

        // Профессиональные данные преподавателя; для студента игнорируются.

        [MaxLength(150)]
        public string? ProfessionalTitle { get; init; }

        [MaxLength(150)]
        public string? Specialization { get; init; }

        public int? YearsOfExperience { get; init; }

        [MaxLength(50)]
        public string? EducationLevel { get; init; }

        [MaxLength(150)]
        public string? Organization { get; init; }

        [MaxLength(500)]
        public string? ProfileUrl { get; init; }
    }
}
