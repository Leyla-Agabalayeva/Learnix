using System;
using System.Collections.Generic;

namespace LMSFinal.Contracts.DTOs.Quizzes
{
    public record QuestionLocalizedDto(
        Guid Id,
        string QuestionText,
        string QuestionType,
        int OrderIndex,
        int Points,
        IReadOnlyList<AnswerLocalizedDto> Answers);
}
