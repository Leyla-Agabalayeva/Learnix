using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Courses
{
    public record UpdateCourseRequest
    {
        public Guid CategoryId { get; init; }

        public string? ThumbnailUrl { get; init; }

        public decimal Price { get; init; }

        public int DurationMinutes { get; init; }

        public CourseLevel Level { get; init; }

        public IReadOnlyList<CourseTranslationInput> Translations { get; init; } = Array.Empty<CourseTranslationInput>();
    }

}
