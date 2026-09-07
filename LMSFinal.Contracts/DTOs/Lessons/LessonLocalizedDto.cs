using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Lessons
{

    public record LessonLocalizedDto(
        Guid Id,
        Guid ModuleId,
        string? VideoUrl,
        int DurationMinutes,
        int OrderIndex,
        bool IsPublished,
        string ResolvedLanguage,
        string Title,
        string? Description,
        string Content,
        Guid? QuizId,


        IReadOnlyList<LessonResourceDto> Resources);
}
