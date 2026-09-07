using System;
using System.Collections.Generic;

namespace LMSFinal.Contracts.DTOs.Quizzes
{

    public record QuizLocalizedDto(
        Guid Id,
        Guid LessonId,
        string Title,
        string? Description,
        int PassingScore,
        int? TimeLimitMinutes,
        IReadOnlyList<QuestionLocalizedDto> Questions);
}
