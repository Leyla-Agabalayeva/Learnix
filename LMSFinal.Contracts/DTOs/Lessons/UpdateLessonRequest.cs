using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Lessons
{
    public record UpdateLessonRequest
    {
        public string? VideoUrl { get; init; }

        public int DurationMinutes { get; init; }

        public bool IsPublished { get; init; }

        public IReadOnlyList<LessonTranslationInput> Translations { get; init; } = Array.Empty<LessonTranslationInput>();
    }
}
