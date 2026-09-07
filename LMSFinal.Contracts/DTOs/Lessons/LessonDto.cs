using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Lessons
{
    public record LessonDto(
        Guid Id,
        Guid ModuleId,
        string? VideoUrl,
        int DurationMinutes,
        int OrderIndex,
        bool IsPublished,
        Guid? QuizId,
        IReadOnlyList<LessonTranslationDto> Translations);
}
