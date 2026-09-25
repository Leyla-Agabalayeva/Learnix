using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Auth
{
    public record RegisterRequest
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; init; } = string.Empty;

        [Required, MinLength(8), MaxLength(100)]
        public string Password { get; init; } = string.Empty;

        [Required, MaxLength(100)]
        public string FirstName { get; init; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; init; } = string.Empty;

        [Required]
        public UserRole Role { get; init; }

        // Профессиональный профиль. Обязателен только при Role == Instructor
        // (правила — в RegisterRequestValidator); студенты эти поля не присылают.

        [MaxLength(1000)]
        public string? Bio { get; init; }

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
