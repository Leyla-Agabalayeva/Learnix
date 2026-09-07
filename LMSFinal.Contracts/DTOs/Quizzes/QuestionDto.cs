using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Quizzes
{
    public record QuestionDto(
       Guid Id,
       string QuestionType,
       int OrderIndex,
       int Points,
       IReadOnlyList<QuestionTranslationDto> Translations,
       IReadOnlyList<AnswerDto> Answers);
}
