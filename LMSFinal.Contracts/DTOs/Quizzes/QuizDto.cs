using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Quizzes
{
    public record QuizDto(
      Guid Id,
      Guid LessonId,
      int PassingScore,
      int? TimeLimitMinutes,
      IReadOnlyList<QuizTranslationDto> Translations,
      IReadOnlyList<QuestionDto> Questions);
}
